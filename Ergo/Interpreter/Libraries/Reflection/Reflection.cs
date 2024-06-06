using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Reflection;

public class Reflection : Library
{
    public override Atom Module => WellKnown.Modules.Reflection;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new AnonymousComplex(),
        new CommaToList(),
        new Compare(),
        new CopyTerm(),
        new Ground(),
        new CurrentModule(),
        new Nonvar(),
        new Number(),
        new NumberVars(),
        new SequenceType(),
        new Term(),
        new TermType(),
        new Variant(),
        new Explain()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
