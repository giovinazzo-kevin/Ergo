using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.String;

public class String : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.String;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new FormatString(),
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
