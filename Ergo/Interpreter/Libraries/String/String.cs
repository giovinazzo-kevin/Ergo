using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.String;

public class String : Library
{
    public override Atom Module => WellKnown.Modules.String;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new FormatString(),
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
