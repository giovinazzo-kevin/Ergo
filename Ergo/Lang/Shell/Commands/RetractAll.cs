﻿namespace Ergo.Lang.ShellCommands
{
    public sealed class RetractAll : AssertShellCommand
    {
        public RetractAll() : base(new[] { "**", "retractall" }, "", true) { }
    }
}
