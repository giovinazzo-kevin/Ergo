using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.IO;

public class IO : Library
{
    public override Atom Module => WellKnown.Modules.IO;
    private readonly BuiltIn[] _exportedBuiltIns = [
        new Write(),
        new WriteCanonical(),
        new WriteQuoted(),
        new WriteDict(),
        new WriteRaw(),
        new Read(),
        new ReadLine(),
        new GetChar(),
        new GetSingleChar(),
        new PeekChar()
    ];
    private readonly InterpreterDirective[] _interpreterDirectives = [
    ];
    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => _interpreterDirectives;
}
