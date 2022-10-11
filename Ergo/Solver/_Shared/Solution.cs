namespace Ergo.Solver;

public readonly struct Solution
{
    public readonly bool IsFailure;

    public readonly SolverScope Scope;
    public readonly Substitution[] Substitutions;
    public readonly Lazy<ImmutableDictionary<ITerm, ITerm>> Links;

    public static Solution Failure(SolverScope scope) => new(scope);
    public static Solution Success(SolverScope scope, params Substitution[] subs) => new(scope, subs);

    /// <summary>
    /// Applies all redundant substitutions and removes them from the set of returned substitutions.
    /// </summary>
    public Solution Simplify()
    {
        return new(Scope, Inner(Substitutions)
            .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false))
            .ToArray())
            ;
        IEnumerable<Substitution> Inner(IEnumerable<Substitution> subs)
        {
            var answers = subs
                .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false));
            var steps = subs
                .Where(s => s.Lhs.Reduce(_ => false, v => v.Ignored, _ => false))
                .ToDictionary(s => s.Lhs);
            foreach (var ans in answers)
            {
                var ret = ans;
                var vars = ret.Rhs.Variables.ToArray();
                while (!ret.Rhs.IsGround)
                {
                    ret = ret.WithRhs(vars.Aggregate(ret.Rhs, (a, b) => steps.ContainsKey(b) ? a.Substitute(steps[b]) : a));
                    var newVars = ret.Rhs.Variables.ToArray();
                    if (newVars.Where(v => vars.Contains(v)).Any())
                        break;

                    vars = newVars;
                }

                yield return ret;
            }
        }
    }

    private Solution(SolverScope scope)
    {
        IsFailure = true;
        Scope = scope;
        Substitutions = Array.Empty<Substitution>();
        Links = new(() => ImmutableDictionary<ITerm, ITerm>.Empty);
    }

    private Solution(SolverScope scope, params Substitution[] subs)
    {
        IsFailure = false;
        Scope = scope;
        Substitutions = subs;
        Links = new(() => ImmutableDictionary<ITerm, ITerm>.Empty
            .AddRange(subs.Select(s => new KeyValuePair<ITerm, ITerm>(s.Lhs, s.Rhs))), true);
    }
}
