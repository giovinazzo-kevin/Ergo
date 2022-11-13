using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries._Stdlib;

public class Stdlib : Library
{
    public override Atom Module => WellKnown.Modules.Stdlib;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        .Append(new DeclareDynamicPredicate())
        .Append(new DeclareModule())
        .Append(new DeclareOperator())
        .Append(new SetModule())
        .Append(new UseModule())
        ;
}
