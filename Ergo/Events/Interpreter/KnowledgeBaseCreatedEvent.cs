﻿using Ergo.Lang.Compiler;

namespace Ergo.Events.Interpreter;

public sealed record class KnowledgeBaseCreatedEvent(KnowledgeBase KnowledgeBase, VMFlags Flags) : ErgoEvent
{
}
