using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class Load : ShellCommand
    {
        public Load()
            : base(new[] { "load" }, "", @"(?<path>.*)", true, 20)
        {
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            shell.Load(ref scope, m.Groups["path"].Value);
            return scope;
        }
    }
}
