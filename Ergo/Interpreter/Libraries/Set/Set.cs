using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class Set : Library
{
    public override Atom Module => WellKnown.Modules.Set;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new Union(),
        new IsSet(),
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
