using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Prologue;

public class Prologue : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Prologue;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new AssertA(),
        new AssertZ(),
        new Cut(),
        new Not(),
        new Retract(),
        new RetractAll(),
        new Unifiable(),
        new Unify(),
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
        new DeclareMetaPredicate()
    ];
    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
