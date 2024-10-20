using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Lambda;

public class Lambda : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Lambda;
    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Runtime.BuiltIns.Lambda(),
    ];
    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => [];
}
