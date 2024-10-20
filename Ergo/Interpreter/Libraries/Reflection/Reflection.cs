using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Reflection;

public class Reflection : IErgoLibrary
{
    public override Atom Module => WellKnown.Modules.Reflection;

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new AnonymousComplex(),
        new CommaToList(),
        new Compare(),
        new CopyTerm(),
        new Ground(),
        new CurrentModule(),
        new Nonvar(),
        new Number(),
        new NumberVars(),
        new SequenceType(),
        new Term(),
        new TermType(),
        new Variant(),
        new Explain(),
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;
}
