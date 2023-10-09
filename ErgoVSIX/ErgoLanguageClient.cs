using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ErgoVSIX
{
    [ContentType("ergo")]
    // [Export(typeof(ILanguageClient))]
    public class ErgoLanguageClient : ILanguageClient
    {
        public string Name => "Ergo Language Extension";
        public IEnumerable<string> ConfigurationSections => null;
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => null;
        public bool ShowNotificationOnInitializeFailed => true;
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();

            var serverPath = Path.Combine(Path.GetDirectoryName(typeof(ErgoVSIXPackage).Assembly.Location), "ErgoLS", "ErgoLS.exe");
            var instances = Process.GetProcessesByName("ErgoLS.exe");
            if (instances.Any())
            {
                foreach (var i in instances.Skip(1))
                {
                    i.Close();
                }
                var I = instances.First();
                return new Connection(I.StandardOutput.BaseStream, I.StandardInput.BaseStream);
            }
            else
            {
                var info = new ProcessStartInfo
                {
                    FileName = serverPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = new Process
                {
                    StartInfo = info
                };
                if (process.Start())
                {
                    return new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
                }
            }
            return null;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            return Task.FromResult(new InitializationFailureContext());
        }
    }
}
