using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Math;

public class Math : Library
{
    public override Atom Module => WellKnown.Modules.Math;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new Eval())
        .Append(new NumberString())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
