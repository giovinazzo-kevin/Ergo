using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Reflection;

public class Reflection : Library
{
    public override Atom Module => WellKnown.Modules.Reflection;

    private readonly BuiltIn[] _exportedBuiltIns = [
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
        new Explain(),
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
