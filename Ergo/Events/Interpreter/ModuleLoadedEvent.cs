using Ergo.Interpreter;

namespace Ergo.Events.Interpreter;

public sealed record class ModuleLoadedEvent(ErgoInterpreter Sender) : ErgoEvent
{
    public InterpreterScope Scope { get; set; } = default!;
}
