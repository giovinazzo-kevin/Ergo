namespace Ergo.Shell.Commands;

public sealed class SolveInteractive : SolveShellCommand
{
    public SolveInteractive() : base([], "Solves the query interactively.", 0, SolveMode.Interactive, ConsoleColor.Black) { }
}
