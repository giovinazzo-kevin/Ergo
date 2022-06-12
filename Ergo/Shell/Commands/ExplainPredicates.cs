namespace Ergo.Shell.Commands;

public sealed class ExplainPredicates : PredicatesShellCommand
{
    public ExplainPredicates() : base(new[] { ":?", "expl" }, "", true) { }
}
