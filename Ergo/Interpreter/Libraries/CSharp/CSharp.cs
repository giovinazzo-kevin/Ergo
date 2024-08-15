using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.CSharp;

public class CSharp : Library
{
    public override Atom Module => WellKnown.Modules.CSharp;
    private readonly BuiltIn[] _exportedBuiltIns = [
        new InvokeOp()
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}