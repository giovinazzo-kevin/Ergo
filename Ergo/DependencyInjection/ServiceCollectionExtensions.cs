using Ergo.Lang.Parser;
using Ergo.Modules.Libraries;
using Ergo.Modules.Libraries._Stdlib;
using Ergo.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Ergo.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddErgoPipeline<TPipeline>(this IServiceCollection services, Func<IServiceProvider, ErgoPipelineBuilder, TPipeline> build)
        where TPipeline : class, IErgoPipeline
    {
        //var envType = typeof(TPipeline)
        //    .GetInterfaces()
        //    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IErgoPipeline<,,>))
        //    .Select(i => i.GetGenericArguments()[2])
        //    .Single();
        //var steps = envType.GetInterfaces()
        //    .SelectMany(envInterface => envInterface.Assembly.GetTypes()
        //        .Where(type => type.GetInterfaces()
        //            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IErgoPipeline<,,>) && i.GetGenericArguments()[2].Equals(envInterface))
        //            .Any());
        return services.AddSingleton(sp => build(sp, new()));
    }

    static IServiceCollection AddErgoLibrary(this IServiceCollection services, Type tLib)
    {
        var exportedDirectives = ErgoLibrary.GetExportedDirectives(tLib);
        var exportedBuiltIns = ErgoLibrary.GetExportedBuiltIns(tLib);
        foreach (var type in exportedDirectives.Concat(exportedBuiltIns))
            services.AddSingleton(type);
        return services
            .AddKeyedSingleton(typeof(IErgoLibrary), tLib.ToModuleName().Explain(), tLib);
    }

    public static IServiceCollection AddErgoLibrary<TLib>(this IServiceCollection services)
        where TLib : class, IErgoLibrary
    {
        return services.AddErgoLibrary(typeof(TLib));
    }

    public static IServiceCollection AddErgoLibraries(this IServiceCollection services, Assembly lookInAssembly)
    {
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(IErgoLibrary)) || !type.IsClass || type.IsAbstract) continue;
            services.AddErgoLibrary(type);
        }
        return services;
    }

    public static IServiceCollection AddErgoAbstractParsers(this IServiceCollection services, Assembly lookInAssembly)
    {
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            if (type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAbstractTermParser<>)) is not { } inter) continue;
            services.AddSingleton(typeof(IAbstractTermParser), type);
            services.AddSingleton(inter, type);
        }
        return services;
    }

    public static IServiceCollection AddErgo(this IServiceCollection services)
        => services
            .AddErgoLibraries(typeof(Stdlib).Assembly)
            .AddErgoAbstractParsers(typeof(ListParser).Assembly)
            .AddTransient<IErgoEnv, ErgoEnv>()
            // -- IBuildModuleTreePipeline --
            .AddSingleton<ILocateModuleStep, LocateModuleStep>()
            .AddSingleton<IStreamFileStep, StreamFileStep>()
            .AddSingleton<IParseStreamStep, ParseStreamStep>()
            .AddSingleton<IBuildModuleTreeStep, BuildModuleTreeStep>()
            .AddErgoPipeline((sp, builder) => builder
                .FixEnvironment<IBuildModuleTreePipeline.Env>()
                .AddStep(sp.GetRequiredService<ILocateModuleStep>())
                .AddStep(sp.GetRequiredService<IStreamFileStep>())
                .AddStep(sp.GetRequiredService<IParseStreamStep>())
                .AddStep(sp.GetRequiredService<IBuildModuleTreeStep>())
                .Cast<IBuildModuleTreePipeline>())
            // -- IBuildDependencyGraphPipeline --
            .AddSingleton<IBuildDependencyGraphStep, BuildDependencyGraphStep>()
            .AddErgoPipeline((sp, builder) => builder
                .FixEnvironment<IBuildDependencyGraphPipeline.Env>()
                .AddStep(sp.GetRequiredService<IBuildModuleTreePipeline>())
                .AddStep(sp.GetRequiredService<IBuildDependencyGraphStep>())
                .Cast<IBuildDependencyGraphPipeline>())
            // -- IBuildExecutionGraphPipeline --
            .AddSingleton<ICompileClauseStep, CompileClauseStep>()
            .AddSingleton<ICompileGoalStep, CompileGoalStep>()
            .AddSingleton<IBuildExecutionGraphStep, BuildExecutionGraphStep>()
            .AddErgoPipeline((sp, builder) => builder
                .FixEnvironment<IBuildExecutionGraphPipeline.Env>()
                .AddStep(sp.GetRequiredService<IBuildDependencyGraphPipeline>())
                .AddStep(sp.GetRequiredService<IBuildExecutionGraphStep>())
                .Cast<IBuildExecutionGraphPipeline>())
        ;
}
