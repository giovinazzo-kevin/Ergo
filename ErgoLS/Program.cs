using Ergo.Facade;
using Ergo.Interpreter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var server = await LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithLoggerFactory(new LoggerFactory())
                .AddDefaultLoggingProvider()
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Trace))
                .WithServices(ConfigureServices)
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<CompletionHandler>()
             );

        await server.WaitForExit;
    }

    static ErgoFacade ConfigureErgoEnvironment()
    {
        return ErgoFacade.Standard;
    }

    static void ConfigureServices(IServiceCollection services)
    {
        var ergo = ConfigureErgoEnvironment();
        var interpreter = ergo.BuildInterpreter(InterpreterFlags.Default);
        services.AddSingleton(interpreter);
        services.AddSingleton<BufferManager>();
        services.AddSingleton<ErgoAutoCompleteService>();
    }
}