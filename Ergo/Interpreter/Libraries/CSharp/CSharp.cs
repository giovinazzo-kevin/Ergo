using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.CSharp;

public class CSharp : Library
{
    public override Atom Module => WellKnown.Modules.CSharp;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new InvokeOp()
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}