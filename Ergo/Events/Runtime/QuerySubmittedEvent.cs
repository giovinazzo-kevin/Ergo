namespace Ergo.Events.Runtime;

public record class QuerySubmittedEvent(ErgoVM VM, Query Query, CompilerFlags Flags) : ErgoEvent;