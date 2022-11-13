using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Math;

public class Math : Library
{
    public override Atom Module => WellKnown.Modules.Math;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new Eval())
        .Append(new Is())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
