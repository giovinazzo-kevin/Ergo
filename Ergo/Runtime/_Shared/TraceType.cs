using System.ComponentModel;

namespace Ergo.Runtime;

public enum TraceType
{
    [Description("Expn")] Expansion,
    [Description("Resv")] BuiltInResolution,
    [Description("Back")] Backtrack,
    [Description("Call")] Call,
    [Description("Exit")] Exit,
    [Description("+TCO")] TailCallOptimization,
}
