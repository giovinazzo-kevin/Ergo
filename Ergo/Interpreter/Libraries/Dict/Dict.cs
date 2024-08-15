using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Dict;

public class Dict : Library
{
    public override Atom Module => WellKnown.Modules.Dict;
    private readonly BuiltIn[] _exportedBuiltIns = [
        new DictKeyValue(),
        new With()
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
