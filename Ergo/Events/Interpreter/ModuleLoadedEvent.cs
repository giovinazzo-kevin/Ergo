using Ergo.Interpreter;

namespace Ergo.Events.Interpreter;

public class ModuleLoadedEvent : ErgoEvent
{
    public ModuleLoadedEvent(ErgoInterpreter sender, InterpreterScope arg) : base(sender, arg) { }
    public new ErgoInterpreter Sender { get => (ErgoInterpreter)base.Sender; protected set => base.Sender = value; }
    public new InterpreterScope Arg { get => (InterpreterScope)base.Arg; set => base.Arg = value; }
}
