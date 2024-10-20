using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Meta;

public class Meta : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Meta;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new BagOf(),
        new For(),
        new Call(),
        new FindAll(),
        new SetOf(),
        new SetupCallCleanup(),
        new Choose()
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => [];
}
