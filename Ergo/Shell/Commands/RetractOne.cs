namespace Ergo.Shell.Commands
{
    public sealed class RetractOne : AssertShellCommand
    {
        public RetractOne() : base(new[] { "*", "retract" }, "", false) { }
    }
}
