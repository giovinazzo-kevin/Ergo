
namespace Ergo.Solver.BuiltIns;

public sealed class Lambda : SolverBuiltIn
{
    public Lambda()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, WellKnown.Modules.Lambda)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args.Length < 2)
        {
            yield return ThrowFalse(scope, SolverError.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(args.Length)).Explain());
            yield break;
        }

        var (parameters, lambda, rest) = (args[0], args[1], args[2..]);
        if (parameters is Variable)
        {
            yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, parameters.Explain());
            yield break;
        }

        // parameters is a plain list of variables; We don't need to capture free variables, unlike SWIPL which is compiled.
        if (!parameters.IsAbstract<List>().TryGetValue(out var list) || list.Contents.Length > rest.Length || list.Contents.Any(x => x is not Variable))
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.LambdaParameters, parameters.Explain());
            yield break;
        }

        var vars = new Dictionary<string, Variable>();
        list = (List)list.Instantiate(scope.InstantiationContext, vars);
        lambda = lambda.Instantiate(scope.InstantiationContext, vars);
        for (var i = 0; i < Math.Min(rest.Length, list.Contents.Length); i++)
        {
            if (list.Contents[i].IsGround)
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.FreeVariable, list.Contents[i].Explain());
                yield break;
            }

            var newSubs = rest[i].Unify(list.Contents[i]).GetOr(new SubstitutionMap());
            lambda = lambda.Substitute(newSubs);
        }

        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : Array.Empty<ITerm>();

        foreach (var eval in new Call().Apply(context, scope, new[] { lambda }.Concat(extraArgs).ToArray()))
        {
            yield return eval;
        }
    }
}
