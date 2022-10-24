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
        {
            sig = args[0].GetSignature();
        }
        scope = scope.WithModule(scope.EntryModule
            .WithTabledPredicate(sig));
        return true;
    }
}
