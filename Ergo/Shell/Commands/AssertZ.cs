namespace Ergo.Shell.Commands
{
    public sealed class AssertZ : AssertShellCommand
    {
        public AssertZ() : base(new[] { "-!", "assertz" }, "", false) { }
    }
}
