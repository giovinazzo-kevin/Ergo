using System;

namespace Ergo.Shell.Commands
{
    public sealed class SolveInteractive : SolveShellCommand
    {
        public SolveInteractive() : base(Array.Empty<string>(), "", 0, true, ConsoleColor.Black) { }
    }
}
