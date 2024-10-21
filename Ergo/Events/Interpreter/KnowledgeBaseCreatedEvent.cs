namespace Ergo.Events.Interpreter;

public sealed record class KnowledgeBaseCreatedEvent(LegacyKnowledgeBase KnowledgeBase, CompilerFlags Flags) : ErgoEvent
{
}
