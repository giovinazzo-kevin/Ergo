using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class List : Library
{
    public override Atom Module => WellKnown.Modules.List;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new Nth0())
        .Append(new Nth1())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
