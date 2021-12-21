namespace Ergo.Lang.ShellCommands
{
    public sealed class AssertA : AssertShellCommand
    {
        public AssertA() : base(new[] { "!-", "asserta" }, "", true) { }
    }
}
