using Ergo.DependencyInjection;
using Ergo.Lang.Ast;
using Ergo.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Ergo;

public static class Program
{
    class TestConsumer(IErgoEnv env, IBuildExecutionGraphPipeline pipeline)
    {
        public void Do()
        {
            var stdlib = pipeline.Run(WellKnown.Modules.Stdlib, env);
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