namespace Ergo.Lang.ShellCommands
{
    public sealed class AssertZ : AssertShellCommand
    {
        public AssertZ() : base(new[] { "-!", "assertz" }, "", false) { }
    }
}
