using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.List;

public class List : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.List;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Nth0(),
        new Nth1(),
        new Sort(),
        new ListSet()
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => [];
}
