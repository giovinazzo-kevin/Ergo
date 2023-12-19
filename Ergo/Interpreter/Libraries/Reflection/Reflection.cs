using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries.Reflection;

public class Reflection : Library
{
    public override Atom Module => WellKnown.Modules.Reflection;
    public override IEnumerable<BuiltIn> GetExportedBuiltins() => Enumerable.Empty<BuiltIn>()
        .Append(new AnonymousComplex())
        .Append(new CommaToList())
        .Append(new Compare())
        .Append(new CopyTerm())
        .Append(new Ground())
        .Append(new CurrentModule())
        .Append(new Nonvar())
        .Append(new Number())
        .Append(new NumberVars())
        .Append(new SequenceType())
        .Append(new Term())
        .Append(new TermType())
        .Append(new Variant())
        .Append(new Explain())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
