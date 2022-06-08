using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class ClearScreen : ShellCommand
    {
        public ClearScreen()
            : base(new[] { "cls" }, "", @"", true, 1000)
        {
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            shell.Clear();
            return scope;
        }
    }
}
