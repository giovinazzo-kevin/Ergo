namespace Ergo.Lang.ShellCommands
{
    public sealed class PrintPredicates : PredicatesShellCommand
    {
        public PrintPredicates() : base(new[] { "::", "desc" }, "", false) { }
    }
}
