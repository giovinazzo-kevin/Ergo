using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.List;

public class Set : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Set;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Union(),
        new IsSet(),
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
