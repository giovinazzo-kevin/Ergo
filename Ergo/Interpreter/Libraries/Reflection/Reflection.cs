using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Reflection;

public class Reflection : Library
{
    public override Atom Module => WellKnown.Modules.Reflection;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new AnonymousComplex())
        .Append(new CommaToList())
        .Append(new Compare())
        .Append(new CopyTerm())
        .Append(new Ground())
        .Append(new Nonvar())
        .Append(new NumberVars())
        .Append(new SequenceType())
        .Append(new Term())
        .Append(new TermType())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
