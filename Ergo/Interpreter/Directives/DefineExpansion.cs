using Ergo.Interpreter.Libraries.Expansions;

namespace Ergo.Interpreter.Directives;

public class DefineExpansion : InterpreterDirective
{
    public DefineExpansion()
        : base("", new("expand"), 1, 20)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        var lib = scope.GetLibrary<Expansions>(WellKnown.Modules.Expansions);
        var visibleModules = scope.VisibleModules;
        var allExpansions = lib.GetDefinedExpansions().Where(x => visibleModules.Contains(x.DeclaringModule));
        var signature = args[0].GetSignature();
        if (WellKnown.Functors.Lambda.Contains(signature.Functor) && signature.Arity.GetOr(default) == 2 && args[0] is Complex cplx)
        {
            // The lambda must have one variable
            if (cplx.Arguments[0] is not List lambdaArgs)
                scope.Throw(InterpreterError.ExpectedTermOfTypeAt, WellKnown.Types.List, cplx.Arguments[0].Explain());
            // There must be only one lambda variable
            else if (lambdaArgs.Contents.Length != 1 || lambdaArgs.Contents[0] is not Variable lambdaVariable)
                scope.Throw(InterpreterError.ExpansionLambdaShouldHaveOneVariable, cplx.Arguments[0].Explain());
            //  The body of the lambda must be a predicate definition
            else if (!Predicate.FromCanonical(cplx.Arguments[1], scope.Entry, out var pred))
                scope.Throw(InterpreterError.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());
            // The lambda variable can't be referenced in the head of the predicate
            else if (pred.Head.Variables.Any(v => v.Name.Equals(lambdaVariable.Name)))
                scope.Throw(InterpreterError.ExpansionHeadCantReferenceLambdaVariable, lambdaVariable.Explain(), pred.Head.Explain());
            // The body of the predicate must reference the lambda variable
            else if (!pred.Body.Variables.Any(v => v.Equals(lambdaVariable)))
                scope.Throw(InterpreterError.ExpansionBodyMustReferenceLambdaVariable, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());
            // The predicate body must contain a reference to all variables that were present in the head
            else if (pred.Head.Variables.Any(v => !v.Ignored && !pred.Body.Variables.Any(w => v.Name.Equals(w.Name))))
                scope.Throw(InterpreterError.ExpansionBodyMustReferenceHeadVariables, WellKnown.Types.Predicate, pred.Explain(false));
            else
            {
                lib.AddExpansion(scope.Entry, lambdaVariable, pred);
                return true;
            }
            return false;
        }

        throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Lambda, args[0].Explain());
    }
}
