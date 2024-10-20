using Ergo.DependencyInjection;
using Ergo.Pipelines.LoadModule;
using Ergo.Lang.Ast;
using Microsoft.Extensions.DependencyInjection;
using Ergo.Pipelines;
using Ergo.Modules;
using Ergo.Compiler;

namespace Ergo;

public static class Program
{
    class TestConsumer(IErgoEnv env, IBuildDependencyGraphPipeline loadModule)
    {
        public void Do()
        {
            var stdlib = loadModule.Run(WellKnown.Modules.Stdlib, env);
            if (stdlib.TryGetB(out var error))
            {
                Console.WriteLine(error.Step.ToString());
                var ex = error.Exception;
                while (ex != null)
                    ex = ex.InnerException;
                Console.WriteLine(error.Exception);
                return;
            }
        }
    }

    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddErgo();
        services.AddTransient<TestConsumer>();
        var serviceProvider = services.BuildServiceProvider();
        var test = serviceProvider.GetRequiredService<TestConsumer>();
        test.Do();
    }
}