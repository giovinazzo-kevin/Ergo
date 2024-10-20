using Ergo.Modules.Libraries.Expansions;

namespace Ergo.Modules.Directives;

public class DefineExpansion() : ErgoDirective("", new("expand"), 1, 20)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        var lib = ctx.ModuleTree.GetLibrary<Expansions>();
        var visibleModules = ctx.ModuleTree.Modules;
        var allExpansions = lib
            .GetDefinedExpansions()
            .Where(x => visibleModules.Keys.Contains(x.DeclaringModule));
        var signature = args[0].GetSignature();
        if (WellKnown.Functors.Lambda.Contains(signature.Functor) && signature.Arity.GetOr(default) == 2 && args[0] is Complex cplx)
        {
            // The lambda must have one variable
            if (cplx.Arguments[0] is not List lambdaArgs)
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.List, cplx.Arguments[0].Explain());
            // There must be only one lambda variable
            else if (lambdaArgs.Contents.Length != 1 || lambdaArgs.Contents[0] is not Variable lambdaVariable)
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpansionLambdaShouldHaveOneVariable, cplx.Arguments[0].Explain());
            //  The body of the lambda must be a predicate definition
            else if (!Clause.FromCanonical(cplx.Arguments[1], ctx.CurrentModule.Name, out var pred))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());
            // The lambda variable can't be referenced in the head of the predicate
            else if (pred.Head.Variables.Any(v => v.Name.Equals(lambdaVariable.Name)))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpansionHeadCantReferenceLambdaVariable, lambdaVariable.Explain(), pred.Head.Explain());
            // The body of the predicate must reference the lambda variable
            else if (!pred.Body.Variables.Any(v => v.Equals(lambdaVariable)))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpansionBodyMustReferenceLambdaVariable, WellKnown.Types.Predicate, cplx.Arguments[1].Explain());
            // The predicate body must contain a reference to all variables that were present in the head
            else if (pred.Head.Variables.Any(v => !v.Ignored && !pred.Body.Variables.Any(w => v.Name.Equals(w.Name))))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpansionBodyMustReferenceHeadVariables, WellKnown.Types.Predicate, pred.Explain(false));
            else
            {
                lib.AddExpansion(ctx.CurrentModule.Name, lambdaVariable, pred);
                return true;
            }
        }
        throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Lambda, args[0].Explain());
    }
}
