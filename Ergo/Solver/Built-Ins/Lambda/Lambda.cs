using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Lambda : BuiltIn
{
    public Lambda()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args.Length < 2)
        {
            solver.Throw(new SolverException(SolverError.UndefinedPredicate, scope, Signature.WithArity(Maybe<int>.Some(args.Length)).Explain()));
            yield return new(WellKnown.Literals.False);
            yield break;
        }

        var (parameters, lambda, rest) = (args[0], args[1], args[2..]);
        if (parameters is Variable)
        {
            solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, parameters.Explain()));
            yield return new Evaluation(WellKnown.Literals.False);
            yield break;
        }

        // parameters is a term in the form {Free}/[List]
        if (parameters is not Complex p
            || !WellKnown.Functors.Division.Contains(p.Functor)
            || !p.Arguments[0].IsAbstractTerm<Set>(out var free)
            || !p.Arguments[1].IsAbstractTerm<List>(out var list))
        {
            solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.LambdaParameters, parameters.Explain()));
            yield return new Evaluation(WellKnown.Literals.False);
            yield break;
        }

        for (var i = 0; i < Math.Min(rest.Length, list.Contents.Length); i++)
        {
            if (list.Contents[i].IsGround)
            {
                solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.FreeVariable, list.Contents[i].Explain()));
                yield return new Evaluation(WellKnown.Literals.False);
                yield break;
            }

            lambda = lambda.Substitute(rest[i].Unify(list.Contents[i]).GetOrThrow());
        }

        await foreach (var eval in new Call().Apply(solver, scope, new[] { lambda }))
        {
            yield return eval;
        }
    }
}
