using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class List : Library
{
    public override Atom Module => WellKnown.Modules.List;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new Nth0(),
        new Nth1(),
        new Sort(),
        new ListSet()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
