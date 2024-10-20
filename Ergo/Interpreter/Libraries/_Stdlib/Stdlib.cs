using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries._Stdlib;

public class Stdlib : IErgoLibrary
{
    public override int LoadOrder => 0;
    public override Atom Module => WellKnown.Modules.Stdlib;

    private readonly ErgoDirective[] _exportedDirectives = [
        new DeclareInlinedPredicate(),
        new DeclareDynamicPredicate(),
        new DeclareModule(),
        new DeclareOperator(),
        new SetModule(),
        new UseModule()
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => [];
    public override IEnumerable<ErgoDirective> ExportedDirectives => _exportedDirectives;
}
