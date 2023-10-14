namespace Ergo.Shell.Commands;

public sealed class SolveTabled : SolveShellCommand
{
    public SolveTabled() : base(new[] { "$" }, "Solves the query in tabled mode.", 5, false, ConsoleColor.Black) { }
}
