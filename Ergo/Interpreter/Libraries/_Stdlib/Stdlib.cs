using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries._Stdlib;

public class Stdlib : Library
{
    public override int LoadOrder => 0;

    public override Atom Module => WellKnown.Modules.Stdlib;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        .Append(new DeclareInlinedPredicate())
        .Append(new DeclareDynamicPredicate())
        .Append(new DeclareModule())
        .Append(new DeclareOperator())
        .Append(new SetModule())
        .Append(new UseModule())
        ;
}
