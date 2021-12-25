using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class Save : ShellCommand
    {
        public Save()
            : base(new[] { "save" }, "", @"(?<path>.*)", 10)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            shell.Save(scope, m.Groups["path"].Value);
        }
    }
}
