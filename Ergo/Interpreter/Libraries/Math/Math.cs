using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Math;

public class Math : Library
{
    public override Atom Module => WellKnown.Modules.Math;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new Eval(),
        new NumberString()
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => [];
}
