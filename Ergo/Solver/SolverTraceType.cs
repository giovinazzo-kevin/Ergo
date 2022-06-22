using System.ComponentModel;

namespace Ergo.Solver;

public enum TraceType
{
    [Description("Expn")] Expansion,
    [Description("Resv")] BuiltInResolution,
    [Description("Call")] Call,
    [Description("Exit")] Exit,
}
