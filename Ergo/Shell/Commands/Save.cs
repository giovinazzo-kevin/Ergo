using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class Save : ShellCommand
    {
        public Save()
            : base(new[] { "save" }, "", @"(?<path>.*)", true, 20)
        {
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            shell.Save(scope, m.Groups["path"].Value);
            return scope;
        }
    }
}
