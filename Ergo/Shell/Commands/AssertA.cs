namespace Ergo.Shell.Commands
{
    public sealed class AssertA : AssertShellCommand
    {
        public AssertA() : base(new[] { "!-", "asserta" }, "", true) { }
    }
}
