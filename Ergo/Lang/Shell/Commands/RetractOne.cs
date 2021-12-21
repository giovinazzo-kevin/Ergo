namespace Ergo.Lang.ShellCommands
{
    public sealed class RetractOne : AssertShellCommand
    {
        public RetractOne() : base(new[] { "*", "retract" }, "", false) { }
    }
}
