using Ergo.Interpreter.Libraries.Meta;

namespace Ergo.Interpreter.Directives;

public class DeclareTabledPredicate : InterpreterDirective
{
    public DeclareTabledPredicate()
        : base("", new("table"), 1, 35)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        scope.GetLibrary<Meta>(WellKnown.Modules.Meta)
            .AddTabledPredicate(scope.Entry, sig);
        return true;
    }
}
