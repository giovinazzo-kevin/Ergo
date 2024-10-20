﻿using Ergo.Modules;

namespace Ergo.Events.Interpreter;

public sealed record class ModuleLoadedEvent(ErgoInterpreter Sender, Atom ModuleName) : ErgoEvent
{
    /// <summary>
    /// The current InterpreterScope. It can be modified to alter module resolution.
    /// </summary>
    public InterpreterScope Scope { get; set; } = default!;
}
