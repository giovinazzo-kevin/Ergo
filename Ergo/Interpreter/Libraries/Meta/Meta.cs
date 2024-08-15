using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Meta;

public class Meta : Library
{
    public override Atom Module => WellKnown.Modules.Meta;

    private readonly BuiltIn[] _exportedBuiltIns = [
        new BagOf(),
        new For(),
        new Call(),
        new FindAll(),
        new SetOf(),
        new SetupCallCleanup(),
        new Choose()
    ];

    public override IEnumerable<BuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<InterpreterDirective> ExportedDirectives => [];
}
