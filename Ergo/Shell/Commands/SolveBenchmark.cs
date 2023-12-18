namespace Ergo.Shell.Commands;

public sealed class SolveBenchmark : SolveShellCommand
{
    public SolveBenchmark() : base(["%"], "Solves the query in benchmark mode.", 6, SolveMode.Benchmark, ConsoleColor.Black) { }
}
