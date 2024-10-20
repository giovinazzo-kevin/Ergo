using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.IO;

public class IO : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.IO;
    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Write(),
        new WriteCanonical(),
        new WriteQuoted(),
        new WriteDict(),
        new WriteRaw(),
        new Read(),
        new ReadLine(),
        new GetChar(),
        new GetSingleChar(),
        new PeekChar()
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
