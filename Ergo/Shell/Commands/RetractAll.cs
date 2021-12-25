namespace Ergo.Shell.Commands
{
    public sealed class RetractAll : AssertShellCommand
    {
        public RetractAll() : base(new[] { "**", "retractall" }, "", true) { }
    }
}
