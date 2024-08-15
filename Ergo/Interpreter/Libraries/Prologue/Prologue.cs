using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Prologue;

public class Prologue : Library
{
    public override Atom Module => WellKnown.Modules.Prologue;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new AssertA(),
        new AssertZ(),
        new Cut(),
        new Not(),
        new Retract(),
        new RetractAll(),
        new Unifiable(),
        new Unify(),
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
        new DeclareMetaPredicate()
    ];
    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
