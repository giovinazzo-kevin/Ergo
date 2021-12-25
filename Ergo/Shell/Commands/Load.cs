using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class Load : ShellCommand
    {
        public Load()
            : base(new[] { "load" }, "", @"(?<path>.*)", 20)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            shell.Load(scope, m.Groups["path"].Value);
        }
    }
}
