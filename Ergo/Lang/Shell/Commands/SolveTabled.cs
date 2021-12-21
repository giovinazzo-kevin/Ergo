using System;

namespace Ergo.Lang.ShellCommands
{
    public sealed class SolveTabled : SolveShellCommand
    {
        public SolveTabled() : base(new[] { "$" }, "", 5, false, ConsoleColor.Black) { }
    }
}
