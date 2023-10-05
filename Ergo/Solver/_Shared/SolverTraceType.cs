using System.ComponentModel;

namespace Ergo.Solver;

public enum SolverTraceType
{
    [Description("Expn")] Expansion,
    [Description("Resv")] BuiltInResolution,
    [Description("Back")] Backtrack,
    [Description("Call")] Call,
    [Description("Exit")] Exit,
    [Description("+TCO")] TailCallOptimization,
}
