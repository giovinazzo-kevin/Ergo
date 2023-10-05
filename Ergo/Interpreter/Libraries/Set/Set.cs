using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class Set : Library
{
    public override Atom Module => WellKnown.Modules.Set;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new Union())
        .Append(new IsSet())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
