using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Math;

public class Math : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Math;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Eval(),
        new NumberString()
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => [];
}
