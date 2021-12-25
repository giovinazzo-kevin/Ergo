using System;

namespace Ergo.Shell.Commands
{
    public sealed class SolveTabled : SolveShellCommand
    {
        public SolveTabled() : base(new[] { "$" }, "", 5, false, ConsoleColor.Black) { }
    }
}
