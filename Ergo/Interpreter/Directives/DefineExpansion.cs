namespace Ergo.Interpreter.Directives;

public class DefineExpansion : InterpreterDirective
{
    public DefineExpansion()
        : base("", new("expand"), 1, 20)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        var allExpansions = scope.GetVisibleModules()
            .SelectMany(x => x.Expansions)
            .ToLookup(l => l.Key);
        var signature = args[0].GetSignature();
        if (WellKnown.Functors.Lambda.Contains(signature.Functor) && signature.Arity.GetOr(default) == 2 && args[0] is Complex cplx)
        {
            // The lambda must have one variable and its body must be a predicate definition that uses that variable.
            if (!cplx.Arguments[0].IsAbstract<List>(out var lambdaArgs))
                scope.Throw(InterpreterError.ExpectedTermOfTypeAt, WellKnown.Types.List, cplx.Arguments[0].Explain());

            if (lambdaArgs.Contents.Length != 1 || lambdaArgs.Contents[0] is not Variable lambdaVariable)
                throw new InterpreterException(InterpreterError.ExpansionLambdaShouldHaveOneVariable, scope, cplx.Arguments[0].Explain());

            if (!Predicate.FromCanonical(cplx.Arguments[1], scope.Entry, out var pred))
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());

            if (!pred.Body.CanonicalForm.Variables.Any(v => v.Equals(lambdaVariable)))
                throw new InterpreterException(InterpreterError.ExpansionIsNotUsingLambdaVariable, scope, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());

            scope = scope.WithModule(scope.EntryModule
                .WithExpansion(lambdaVariable, pred));
            return true;
        }

        throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Lambda, args[0].Explain());
    }
}
