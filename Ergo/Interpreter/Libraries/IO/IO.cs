using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.IO;

public class IO : Library
{
    public override Atom Module => WellKnown.Modules.IO;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => [
        new Write(),
        new WriteCanonical(),
        new WriteQuoted(),
        new WriteDict(),
        new WriteRaw(),
        new Read(),
        new ReadLine(),
        new GetChar(),
        new GetSingleChar(),
        new PeekChar(),
    ];
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => [];
}
