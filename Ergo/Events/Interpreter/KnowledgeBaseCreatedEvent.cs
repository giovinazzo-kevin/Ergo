namespace Ergo.Events.Interpreter;

public sealed record class KnowledgeBaseCreatedEvent(ErgoKnowledgeBase KnowledgeBase, CompilerFlags Flags) : ErgoEvent
{
}
