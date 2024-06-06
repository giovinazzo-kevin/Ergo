using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.List;

public class Set : Library
{
    public override Atom Module => WellKnown.Modules.Set;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new Union(),
        new IsSet()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
