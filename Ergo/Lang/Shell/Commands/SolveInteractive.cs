using System;

namespace Ergo.Lang.ShellCommands
{
    public sealed class SolveInteractive : SolveShellCommand
    {
        public SolveInteractive() : base(Array.Empty<string>(), "", 0, true, ConsoleColor.Black) { }
    }
}
