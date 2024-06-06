using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Prologue;

public class Prologue : Library
{
    public override Atom Module => WellKnown.Modules.Prologue;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new AssertA(),
        new AssertZ(),
        new Cut(),
        new Not(),
        new Retract(),
        new RetractAll(),
        new Unifiable(),
        new Unify()
    ];

    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [
        new DeclareMetaPredicate()
    ];
}
