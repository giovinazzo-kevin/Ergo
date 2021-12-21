using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class Save : ShellCommand
    {
        public Save()
            : base(new[] { "save" }, "", @"(?<path>.*)", 10)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            s.Save(m.Groups["path"].Value);
        }
    }
}
