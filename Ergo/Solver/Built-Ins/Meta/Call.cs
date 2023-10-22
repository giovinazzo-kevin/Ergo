
namespace Ergo.Solver.BuiltIns;

public sealed class Call : SolverBuiltIn
{
    public Call()
        : base("", new("call"), Maybe<int>.None, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
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

        if (goal is not NTuple comma)
        {
            comma = new(ImmutableArray<ITerm>.Empty.Add(goal), goal.Scope);
        }

        var any = false;
        foreach (var solution in context.Solver.Solve(new(comma), scope))
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
