using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Meta;

public class Meta : Library
{
    public override Atom Module => WellKnown.Modules.Meta;

    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new BagOf())
        .Append(new Call())
        .Append(new FindAll())
        .Append(new SetOf())
        .Append(new SetupCallCleanup())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
