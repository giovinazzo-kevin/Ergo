using Ergo.Pipelines.LoadModule;
using Ergo.Lang.Extensions;
using Ergo.Modules.Libraries;
using Ergo.Modules.Libraries._Stdlib;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Ergo.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddErgoPipeline<TPipeline>(this IServiceCollection services, Func<IServiceProvider, ErgoPipelineBuilder, TPipeline> build)
        where TPipeline : class, IErgoPipeline
        => services
            .AddSingleton(sp => build(sp, new()))
        ;

    public static IServiceCollection AddErgoLibrary<TLib>(this IServiceCollection services)
        where TLib : class, IErgoLibrary
        => services
            .AddKeyedSingleton<IErgoLibrary, TLib>(typeof(TLib).Name.ToErgoCase())
        ;

    public static IServiceCollection AddErgoLibraries(this IServiceCollection services, Assembly lookInAssembly)
    {
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(IErgoLibrary))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            services.AddKeyedSingleton(typeof(IErgoLibrary), type.Name.ToErgoCase(), type);
        }
        return services;
    }

    public static IServiceCollection AddErgo(this IServiceCollection services) 
        => services
            .AddErgoLibraries(typeof(Stdlib).Assembly)
            .AddErgoPipeline((sp, builder) => builder
                .FixEnvironment<ILoadModulePipeline.Env>()
                .AddStep(sp.GetRequiredService<ILocateModuleStep>())
                .AddStep(sp.GetRequiredService<IStreamFileStep>())
                .AddStep(sp.GetRequiredService<IBuildModuleTreeStep>())
                .Cast<ILoadModulePipeline>())
        ;
}
