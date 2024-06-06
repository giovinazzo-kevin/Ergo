using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Dict;

public class Dict : Library
{
    public override Atom Module => WellKnown.Modules.Dict;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new DictKeyValue(),
        new With()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
