using Ergo.Lang;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using System.Reflection;
using System.Reflection.Emit;

namespace Ergo;

public sealed class ErgoPipelineBuilder
{
    public ErgoPipelineBuilder<TEnv> FixEnvironment<TEnv>()
        => new();
}

public sealed class ErgoPipelineBuilder<TEnv>
{
    public ErgoPipelineBuilder<TInput, TInput, TOutput, TEnv> AddStep<TInput, TOutput>(IErgoPipeline<TInput, TOutput, TEnv> step)
        => new([], step);
}

public sealed class ErgoPipelineBuilder<TInput, TInter, TOutput, TEnv>(IErgoPipeline[] prev, IErgoPipeline<TInter, TOutput, TEnv> curr) 
    : IErgoPipeline<TInput, TOutput, TEnv>
{
    private static readonly MethodInfo ExecuteStep = typeof(IErgoPipeline<,,>)
        .GetMethod(nameof(IErgoPipeline<Unit, Unit, Unit>.Run), BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException();
    private static readonly MethodInfo TryGetResult = typeof(Either<,>)
        .GetMethod(nameof(Either<Unit, Unit>.TryGetA), BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException();
    private static readonly PropertyInfo Error = typeof(Either<,>)
        .GetProperty("B", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException();
    private static readonly MethodInfo CreateResult = typeof(Either<,>)
        .GetMethod(nameof(Either<Unit, Unit>.FromA), BindingFlags.Static | BindingFlags.Public)
        ?? throw new InvalidOperationException();
    private readonly Dictionary<Type, object> ProxyCache = [];

    private readonly IErgoPipeline[] Steps = prev.Append(curr).ToArray();
    private readonly (MethodInfo ExecuteStep, MethodInfo TryGetResult, MethodInfo CreateResult)[] MethodTable =
        prev.Append(curr)
            .Select((step, i) =>
            {
                var executeStep = ExecuteStep
                    .MakeGenericMethod(step.InterType, step.OutputType);
                var tryGetResult = TryGetResult
                    .MakeGenericMethod(step.InterType, step.OutputType);
                var createResult = CreateResult
                    .MakeGenericMethod(step.OutputType, typeof(PipelineError));
                return (executeStep, tryGetResult, createResult);
            })
        .ToArray();

    public ErgoPipelineBuilder<TInput, TOutput, TNext, TEnv> AddStep<TNext>(IErgoPipeline<TOutput, TNext, TEnv> next)
        => new ([ ..prev, curr], next);

    public Either<TOutput, PipelineError> Run(TInput input, TEnv environment)
    {
        var result = new object[1];
        var data = (object)Either<TInput, PipelineError>.FromA(input);
        for (int i = 0; i < Steps.Length; i++)
        {
            ref var step = ref Steps[i];
            try
            {
                data = MethodTable[i].ExecuteStep
                    .Invoke(step, [data, environment])!;
                var hasResult = (bool)MethodTable[i].TryGetResult
                    .Invoke(data, result)!;
                if (!hasResult)
                {
                    var error = (PipelineError)Error.GetValue(data)!;
                    return Either<TOutput, PipelineError>.FromB(error);
                }
                data = MethodTable[i].CreateResult
                    .Invoke(null, result);
            }
            catch (Exception ex)
            {
                var error = new PipelineError(step, ex);
                return Either<TOutput, PipelineError>.FromB(error);
            }
        }
        return Either<TOutput, PipelineError>.FromA((TOutput)data!);
    }

    public TInterface Cast<TInterface>()
        where TInterface : IErgoPipeline<TInput, TOutput, TEnv>
    {
        if (this is TInterface self) 
            return self;

        if (ProxyCache.TryGetValue(typeof(TInterface), out var proxy))
            return (TInterface)proxy;

        var assemblyName = new AssemblyName($"{typeof(TInterface).Name}ProxyAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{typeof(TInterface).Name}ProxyModule");

        // Define a new type that implements TInterface
        var typeBuilder = moduleBuilder.DefineType("DynamicProxy", TypeAttributes.Public);
        typeBuilder.AddInterfaceImplementation(typeof(TInterface));
        // Create a field to hold the reference to the builder
        var builderField = typeBuilder.DefineField("_builder", typeof(ErgoPipelineBuilder<TInput, TInter, TOutput, TEnv>), FieldAttributes.Private);

        // Create a constructor to initialize the builder field
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(ErgoPipelineBuilder<TInput, TInter, TOutput, TEnv>)]);

        var il = constructorBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, builderField);
        il.Emit(OpCodes.Ret);

        // Implement the Execute method
        var executeMethod = typeof(TInterface).GetMethod(nameof(IErgoPipeline<TInput, TOutput, TEnv>.Run))!;
        var methodBuilder = typeBuilder.DefineMethod(
            executeMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            executeMethod.ReturnType,
            executeMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        il = methodBuilder.GetILGenerator();
        // Load the input and environment arguments
        for (int i = 0; i < executeMethod.GetParameters().Length; i++)
            il.Emit(OpCodes.Ldarg, i);

        // Load the builder field and call Execute on it
        il.Emit(OpCodes.Ldfld, builderField);
        il.Emit(OpCodes.Callvirt, typeof(ErgoPipelineBuilder<TInput, TInter, TOutput, TEnv>).GetMethod("Execute")!); // Call Execute on the builder
        il.Emit(OpCodes.Ret);

        // Create the type
        var proxyType = typeBuilder.CreateType();

        // Create an instance of the proxy type and return it
        return (TInterface)(ProxyCache[typeof(TInterface)] = Activator.CreateInstance(proxyType, this)!);
    }

}