using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class Load : ShellCommand
    {
        public Load()
            : base(new[] { "load" }, "", @"(?<path>.*)", 10)
        {
        }

        public override void Callback(ErgoShell s, Match m)
        {
            s.Load(m.Groups["path"].Value);
        }
    }
}
