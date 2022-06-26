
namespace Ergo.Solver.BuiltIns;

public sealed class Call : SolverBuiltIn
{
    public Call()
        : base("", new("call"), Maybe<int>.None, WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        scope = scope.WithDepth(scope.Depth + 1)
            .WithCaller(scope.Callee)
            .WithCallee(GetStub(args));
        if (args.Length == 0)
        {
            yield return ThrowFalse(scope, SolverError.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(0)).Explain());
            yield break;
        }

        var goal = args.Aggregate((a, b) => a.Concat(b));
        if (goal is Variable)
        {
            yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, goal.Explain());
            yield break;
        }

        if (!goal.IsAbstract<NTuple>(out var comma))
        {
            comma = new(ImmutableArray<ITerm>.Empty.Add(goal));
        }

        var any = false;
        await foreach (var solution in solver.Solve(new(comma), scope))
        {
            yield return True(solution.Substitutions);
            any = true;
        }

        if (!any)
        {
            yield return False();
        }
    }
}
