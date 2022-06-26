namespace Ergo.Interpreter.Directives;

public class DeclareDynamicPredicate : InterpreterDirective
{
    public DeclareDynamicPredicate()
        : base("", new("dynamic"), 1, 30)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
        {
            sig = args[0].GetSignature();
        }

        scope = scope.WithModule(scope.EntryModule
            .WithDynamicPredicate(sig));
        return true;
    }
}
