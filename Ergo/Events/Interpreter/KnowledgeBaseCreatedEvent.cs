using Ergo.Interpreter;

namespace Ergo.Events.Interpreter;

public sealed record class KnowledgeBaseCreatedEvent(InterpreterScope Scope, KnowledgeBase KnowledgeBase) : ErgoEvent
{
}
