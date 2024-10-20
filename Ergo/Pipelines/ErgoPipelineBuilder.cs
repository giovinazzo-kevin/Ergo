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

    private static readonly ModuleBuilder ModuleBuilder;
    static ErgoPipelineBuilder()
    {
        var assemblyName = new AssemblyName($"DynamicPipelineProxies");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
    }

    private static readonly PropertyInfo Error = typeof(Either<,>)
        .GetProperty("B", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException();
    private readonly Dictionary<Type, object> ProxyCache = [];

    private readonly IErgoPipeline[] Steps = [.. prev, curr];
    private readonly (MethodInfo ExecuteStep, PropertyInfo HasResult, PropertyInfo GetError, PropertyInfo GetResult, MethodInfo CreateResult)[] MethodTable =
        prev.Append(curr)
            .Select((step, i) =>
            {
                var executeStep = typeof(IErgoPipeline<,,>)
                    .MakeGenericType(step.InterType, step.OutputType, step.EnvType)
                    .GetMethod(nameof(IErgoPipeline<Unit, Unit, Unit>.Run), BindingFlags.Instance | BindingFlags.Public);
                var either = typeof(Either<,>)
                    .MakeGenericType(step.OutputType, typeof(PipelineError));
                var hasResult = either
                    .GetProperty("IsA", BindingFlags.Instance | BindingFlags.Public);
                var getResult = either
                    .GetProperty("A", BindingFlags.Instance | BindingFlags.NonPublic);
                var getError = either
                    .GetProperty("B", BindingFlags.Instance | BindingFlags.NonPublic);
                var createResult = either
                    .GetMethod(nameof(Either<Unit, Unit>.FromA), BindingFlags.Static | BindingFlags.Public);
                return (executeStep, hasResult, getError, getResult, createResult);
            })
        .ToArray();

    public ErgoPipelineBuilder<TInput, TOutput, TNext, TEnv> AddStep<TNext>(IErgoPipeline<TOutput, TNext, TEnv> next)
        => new ([ ..prev, curr], next);

    public Either<TOutput, PipelineError> Run(TInput input, TEnv environment)
    {
        var result = new object[1] { input };
        var data = (object)Either<TInput, PipelineError>.FromA(input);
        for (int i = 0; i < Steps.Length; i++)
        {
            ref var step = ref Steps[i];
            try
            {
                data = MethodTable[i].ExecuteStep
                    .Invoke(step, [result[0], environment])!;
                var hasResult = (bool)MethodTable[i].HasResult
                    .GetValue(data);
                if (!hasResult)
                {
                    var error = (PipelineError)MethodTable[i].GetError.GetValue(data)!;
                    return Either<TOutput, PipelineError>.FromB(error);
                }
                result[0] = MethodTable[i].GetResult
                    .GetValue(data);
                data = MethodTable[i].CreateResult
                    .Invoke(null, result);
            }
            catch (Exception ex)
            {
                var error = new PipelineError(step, ex);
                return Either<TOutput, PipelineError>.FromB(error);
            }
        }
        return (Either<TOutput, PipelineError>)(data);
    }

    public TInterface Cast<TInterface>()
        where TInterface : IErgoPipeline<TInput, TOutput, TEnv>
    {
        if (this is TInterface self) 
            return self;

        if (ProxyCache.TryGetValue(typeof(TInterface), out var proxy))
            return (TInterface)proxy;

        // Define a new type that implements TInterface
        var typeBuilder = ModuleBuilder.DefineType($"PipelineProxy__{typeof(TInterface).Name}", TypeAttributes.Public);
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

        var executeMethodName = nameof(IErgoPipeline<TInput, TOutput, TEnv>.Run);
        // Implement the Execute method
        var executeMethodOnInterface = typeof(IErgoPipeline<TInput, TOutput, TEnv>).GetMethod(executeMethodName)!;
        var methodBuilder = typeBuilder.DefineMethod(
            executeMethodOnInterface.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            executeMethodOnInterface.ReturnType,
            executeMethodOnInterface.GetParameters().Select(p => p.ParameterType).ToArray());

        var executeMethodOnBuilder = typeof(ErgoPipelineBuilder<TInput, TInter, TOutput, TEnv>).GetMethod(executeMethodName)!;
        il = methodBuilder.GetILGenerator();
        // Load the proxy instance (this)
        il.Emit(OpCodes.Ldarg_0);
        // Load the builder field (from 'this')
        il.Emit(OpCodes.Ldfld, builderField);
        // Load the input and environment arguments
        il.Emit(OpCodes.Ldarg_1); // Assuming the first argument is the input
        il.Emit(OpCodes.Ldarg_2); // Assuming the second argument is the environment
        // Call the 'Run' method on the builder
        il.Emit(OpCodes.Callvirt, executeMethodOnBuilder); // Call 'Run' on the builder
        // Return the result
        il.Emit(OpCodes.Ret);

        // Create the type
        var proxyType = typeBuilder.CreateType();

        // Create an instance of the proxy type and return it
        return (TInterface)(ProxyCache[typeof(TInterface)] = Activator.CreateInstance(proxyType, this)!);
    }

}