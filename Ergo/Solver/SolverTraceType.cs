using System.ComponentModel;

namespace Ergo.Solver;

public enum SolverTraceType
{
    [Description("Expn")] Expansion,
    [Description("Resv")] BuiltInResolution,
    [Description("Call")] Call,
    [Description("Exit")] Exit,
}
