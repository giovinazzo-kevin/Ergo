using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class Load : ShellCommand
    {
        public Load()
            : base(new[] { "load" }, "", @"(?<path>.*)", 10)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            s.Load(m.Groups["path"].Value);
        }
    }
}
