using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public sealed class NumberVars : SolverBuiltIn
{
    public NumberVars()
        : base("", new("numbervars"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var allSubs = new SubstitutionMap();
        var (start, end) = (0, 0);
        if (args[1].Unify(new Atom(start)).TryGetValue(out var subs1))
        {
            if (!args[1].IsGround)
            {
                allSubs.AddRange(subs1);
            }
        }
        else if (args[1].IsGround && args[1] is Atom { Value: EDecimal d })
        {
            start = d.ToInt32IfExact();
        }
        else if (args[1] is not Atom)
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            yield break;
        }


        var newVars = new Dictionary<string, Variable>();
        foreach (var (v, i) in args[0].Variables.Select((v, i) => (v, i)))
        {
            newVars[v.Name] = new Variable($"$VAR({i})");
            ++end;
        }

        if (!args[0].Instantiate(scope.InstantiationContext, newVars).Unify(args[0]).TryGetValue(out var subs0))
        {
            yield return False();
            yield break;
        }
        allSubs.AddRange(subs0);

        if (args[2].Unify(new Atom(end)).TryGetValue(out var subs2))
        {
            if (!args[2].IsGround)
            {
                allSubs.AddRange(subs2);
            }
        }
        else if (args[2].IsGround && args[2] is Atom { Value: EDecimal d } && d.ToInt32IfExact() != end)
        {
            yield return False();
            yield break;
        }
        else if (args[1] is not Atom)
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            yield break;
        }

        yield return True(allSubs);
    }
}
