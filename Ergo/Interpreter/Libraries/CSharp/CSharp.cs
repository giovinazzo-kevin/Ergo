using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.CSharp;

public class CSharp : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.CSharp;
    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new InvokeOp()
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}