using Ergo.Interpreter.Libraries.Tabling;

namespace Ergo.Interpreter.Directives;

public class DeclareTabledPredicate : InterpreterDirective
{
    public DeclareTabledPredicate()
        : base("", "table", 1, 35)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        scope.GetLibrary<Tabling>(WellKnown.Modules.Tabling)
            .AddTabledPredicate(scope.Entry, sig);
        return true;
    }
}
