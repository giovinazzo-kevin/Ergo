using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Prologue;

public class Prologue : Library
{
    public override Atom Module => WellKnown.Modules.Prologue;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new AssertA())
        .Append(new AssertZ())
        .Append(new Cut())
        .Append(new Not())
        .Append(new Retract())
        .Append(new RetractAll())
        .Append(new Unifiable())
        .Append(new Unify())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        .Append(new DeclareMetaPredicate())
        ;
}
