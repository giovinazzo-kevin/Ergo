using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Dict;

public class Dict : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Dict;
    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new DictKeyValue(),
        new With()
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
