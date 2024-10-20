using Ergo.DependencyInjection;
using Ergo.Pipelines.LoadModule;
using Ergo.Lang.Ast;
using Microsoft.Extensions.DependencyInjection;

namespace Ergo;

public static class Program
{
    class TestConsumer(IErgoEnv env, ILoadModulePipeline loadModule)
    {
        public void Do()
        {
            var stdlib = loadModule.Execute(WellKnown.Modules.Stdlib, env);
        }
    }

    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddErgo();
        services.AddSingleton<TestConsumer>();
        var serviceProvider = services.BuildServiceProvider();
        var test = serviceProvider.GetRequiredService<TestConsumer>();
        test.Do();
    }
}