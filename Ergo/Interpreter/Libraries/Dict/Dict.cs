using Ergo.Interpreter.Directives;
using Ergo.VM.BuiltIns;

namespace Ergo.Interpreter.Libraries.Dict;

public class Dict : Library
{
    public override Atom Module => WellKnown.Modules.Dict;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new DictKeyValue())
        .Append(new With())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
