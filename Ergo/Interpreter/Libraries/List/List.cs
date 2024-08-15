using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class List : Library
{
    public override Atom Module => WellKnown.Modules.List;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new Nth0(),
        new Nth1(),
        new Sort(),
        new ListSet()
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => [];
}
