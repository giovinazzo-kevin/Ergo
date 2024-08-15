using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Lambda;

public class Lambda : Library
{
    public override Atom Module => WellKnown.Modules.Lambda;
    private readonly BuiltIn[] _exportedBuiltIns = [
        new Runtime.BuiltIns.Lambda(),
    ];
    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => [];
}
