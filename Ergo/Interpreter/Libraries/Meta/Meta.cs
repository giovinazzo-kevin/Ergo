using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Meta;

public class Meta : Library
{
    public override Atom Module => WellKnown.Modules.Meta;


    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new BagOf())
        .Append(new For())
        .Append(new Call())
        .Append(new FindAll())
        .Append(new SetOf())
        .Append(new SetupCallCleanup())
        .Append(new Choose())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
