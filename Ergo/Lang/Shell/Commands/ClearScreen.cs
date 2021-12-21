using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class ClearScreen : ShellCommand
    {
        public ClearScreen()
            : base(new[] { "cls" }, "", @"", 10)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            s.Clear();
        }
    }
}
