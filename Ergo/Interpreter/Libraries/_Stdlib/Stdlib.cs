using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries._Stdlib;

public class Stdlib : Library
{
    public override int LoadOrder => 0;
    public override Atom Module => WellKnown.Modules.Stdlib;

    private readonly InterpreterDirective[] _exportedDirectives = [
        new DeclareInlinedPredicate(),
        new DeclareDynamicPredicate(),
        new DeclareModule(),
        new DeclareOperator(),
        new SetModule(),
        new UseModule()
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => [];
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _exportedDirectives;
}
