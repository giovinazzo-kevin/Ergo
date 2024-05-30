namespace Ergo.Shell.Commands;

public sealed class PrintPredicates : PredicatesShellCommand
{
    public PrintPredicates() : base(["::", "desc"], "Displays the head of all matching predicates.", false) { }
}
