namespace Ergo.Solver;

public readonly struct Solution
{
    public readonly SolverScope Scope;
    public readonly SubstitutionMap Substitutions;
    public readonly Lazy<ImmutableDictionary<ITerm, ITerm>> Links;
    public Solution(SolverScope scope, SubstitutionMap subs)
    {
        Scope = scope;
        Substitutions = subs;
        Links = new(() => ImmutableDictionary<ITerm, ITerm>.Empty
            .AddRange(subs.Select(s => new KeyValuePair<ITerm, ITerm>(s.Lhs, s.Rhs))), true);
    }

    public Solution AddSubstitutions(ref SubstitutionMap subs) => new(Scope, SubstitutionMap.MergeRef(ref subs, Substitutions));
    public Solution AddSubstitutions(SubstitutionMap subs) => new(Scope, SubstitutionMap.MergeCopy(subs, Substitutions));

    /// <summary>
    /// Applies all redundant substitutions and removes them from the set of returned substitutions.
    /// </summary>
    public static IEnumerable<Substitution> Simplify(IEnumerable<Substitution> subs)
    {
        var answers = new Queue<Substitution>();
        var retry = new Queue<Substitution>();
        var output = new HashSet<Substitution>();
        subs = subs.Distinct();
        foreach (var s in subs
            .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false)))
            answers.Enqueue(s);
        var steps = subs
            .Where(s => s.Lhs.Reduce(_ => false, v => v.Ignored, _ => false))
            .ToDictionary(s => s.Lhs);
        while (answers.TryDequeue(out var ans))
        {
            var ret = ans;
            var vars = ret.Rhs.Variables.ToArray();
            while (!ret.Rhs.IsGround)
            {
                ret = ret.WithRhs(vars.Aggregate(ret.Rhs, (a, b) => steps.ContainsKey(b) ? a.Substitute(steps[b]) : a));
                var newVars = ret.Rhs.Variables.ToArray();
                var unresolvedVars = newVars.Where(v => vars.Contains(v));
                if (unresolvedVars.Any())
                {
                    retry.Enqueue(ret);
                    break;
                }
                vars = newVars;
            }
            output.Add(ret);
        }

        while (retry.TryDequeue(out var ans))
        {
            var subs_ = output.Where(x => x.Lhs.Equals(ans.Lhs) && x.Rhs is Variable)
                .Select(x => output.FirstOrDefault(y => y.Rhs.Equals(x.Rhs) && !y.Lhs.Equals(x.Lhs)))
                .ToList();
            if (!subs_.Any())
                continue;
            var sub = subs_.First();
            output.Remove(output.First(x => x.Lhs.Equals(ans.Lhs)));
            if (sub.Lhs is null)
                continue;
            output.Add(new Substitution(ans.Lhs, sub.Lhs));
        }

        return output;
    }

    public Solution Simplify()
    {
        return new(Scope, new(Simplify(Substitutions)
            .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false))))
            ;
    }
}
