namespace Ergo.Shell.Commands;

public sealed class SolveTabled : SolveShellCommand
{
    public SolveTabled() : base(["$"], "Solves the query in tabled mode.", 5, SolveMode.Tabled, ConsoleColor.Black) { }
}
