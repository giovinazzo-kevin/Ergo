using Ergo.Lang.Compiler;

namespace Ergo.Events.VM;

public record class QuerySubmittedEvent(ErgoVM VM, Query Query, VMFlags Flags) : ErgoEvent;