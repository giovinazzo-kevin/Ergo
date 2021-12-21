namespace Ergo.Lang.ShellCommands
{
    public sealed class ExplainPredicates : PredicatesShellCommand
    {
        public ExplainPredicates() : base(new[] { ":?", "expl" }, "", true) { }
    }
}
