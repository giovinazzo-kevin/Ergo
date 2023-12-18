using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.String;

public class String : Library
{
    public override Atom Module => WellKnown.Modules.String;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new FormatString())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
