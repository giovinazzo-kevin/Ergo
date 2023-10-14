namespace Ergo.Shell.Commands;

public sealed class SolveInteractive : SolveShellCommand
{
    public SolveInteractive() : base(Array.Empty<string>(), "Solves the query interactively.", 0, true, ConsoleColor.Black) { }
}
