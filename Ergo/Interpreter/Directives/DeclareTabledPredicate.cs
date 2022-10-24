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

        var expansionVar = new Variable("O");
        var anonTerm = sig.Functor.BuildAnonymousTerm(sig.Arity.GetOr(0));
        var cplx = ((ITerm)new Complex(new Atom("tabled"), sig.Functor, new List(anonTerm.Variables.Cast<ITerm>()).CanonicalForm))
            .Qualified(WellKnown.Modules.Meta);

        var expansionPred = new Predicate(
            "rewrite rule for tabled predicates",
            scope.EntryModule.Name,
            anonTerm,
            new NTuple(new ITerm[] { new Complex(new Atom("="), expansionVar, cplx).AsOperator(OperatorAffix.Infix) }),
            dynamic: true,
            exported: true);
        scope = scope.WithModule(scope.EntryModule
            .WithTabledPredicate(sig)
            .WithExpansion(expansionVar, expansionPred));
        return true;
    }
}
