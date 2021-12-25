using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ClearScreen : ShellCommand
    {
        public ClearScreen()
            : base(new[] { "cls" }, "", @"", 10)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            shell.Clear();
        }
    }
}
