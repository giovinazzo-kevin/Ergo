using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class Save : ShellCommand
    {
        public Save()
            : base(new[] { "save" }, "", @"(?<path>.*)", 10)
        {
        }

        public override void Callback(ErgoShell s, Match m)
        {
            s.Save(m.Groups["path"].Value);
        }
    }
}
