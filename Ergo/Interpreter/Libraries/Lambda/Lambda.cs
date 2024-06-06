using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Lambda;
public class Lambda : Library
{
    public override Atom Module => WellKnown.Modules.Lambda;

    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new Runtime.BuiltIns.Lambda()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
