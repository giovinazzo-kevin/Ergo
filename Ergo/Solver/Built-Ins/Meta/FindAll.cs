
namespace Ergo.Solver.BuiltIns;

public sealed class FindAll : SolverBuiltIn
{
    public FindAll()
        : base("", new("findall"), 3, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        scope = scope.WithDepth(scope.Depth + 1)
            .WithCaller(scope.Callee)
            .WithCallee(GetStub(args));
        if (args[1] is not NTuple comma)
        {
            comma = new(ImmutableArray<ITerm>.Empty.Add(args[1]), default);
        }

        var solutions = context.Solver.Solve(new(comma), scope)
            .ToArray();
        if (solutions.Length == 0)
        {
            if (args[2].IsGround && args[2].Equals(WellKnown.Literals.EmptyList))
            {
                yield return new Evaluation(WellKnown.Literals.True);
            }
            else if (!args[2].IsGround)
            {
                yield return True(new Substitution(args[2], WellKnown.Literals.EmptyList));
            }
            else
            {
                yield return False();
            }
        }
        else
        {
            var list = new List(ImmutableArray.CreateRange(solutions.Select(s => args[0].Substitute(s.Substitutions))), default, default);
            if (args[2].IsGround && args[2].Equals(list))
            {
                yield return new Evaluation(WellKnown.Literals.True);
            }
            else if (!args[2].IsGround)
            {
                yield return True(new Substitution(args[2], list));
            }
            else
            {
                yield return False();
            }
        }
    }
}
