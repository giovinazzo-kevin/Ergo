using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Lambda : BuiltIn
{
    public Lambda()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, Modules.Lambda)
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

        // parameters is a plain list of variables; We don't need to capture free variables, unlike SWIPL which is compiled.
        if (!parameters.IsAbstractTerm<List>(out var list) || list.Contents.Length > rest.Length || list.Contents.Any(x => x is not Variable))
        {
            solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.LambdaParameters, parameters.Explain()));
            yield return new Evaluation(WellKnown.Literals.False);
            yield break;
        }

        var (ctx, vars) = (new InstantiationContext("L"), new Dictionary<string, Variable>());
        list = (List)list.Instantiate(ctx, vars);
        lambda = lambda.Instantiate(ctx, vars);
        for (var i = 0; i < Math.Min(rest.Length, list.Contents.Length); i++)
        {
            if (list.Contents[i].IsGround)
            {
                solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.FreeVariable, list.Contents[i].Explain()));
                yield return new Evaluation(WellKnown.Literals.False);
                yield break;
            }

            var newSubs = rest[i].Unify(list.Contents[i]).GetOrThrow();
            lambda = lambda.Substitute(newSubs);
        }

        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : Array.Empty<ITerm>();

        await foreach (var eval in new Call().Apply(solver, scope, new[] { lambda }.Concat(extraArgs).ToArray()))
        {
            yield return eval;
        }
    }
}
