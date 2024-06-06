namespace Ergo.Runtime;

public readonly struct Solution
{
    public readonly SubstitutionMap Substitutions;
    public IEnumerable<Variable> Variables => Substitutions.SelectMany(x => x.Rhs.Variables).Distinct();

    public Solution(SubstitutionMap subs)
    {
        Substitutions = subs;
    }
    public Solution Clone() => new(SubstitutionMap.MergeRef([], Substitutions));
    public Solution PrependSubstitutions(SubstitutionMap subs) => new(SubstitutionMap.MergeRef(Substitutions, subs));
    public Solution AppendSubstitutions(SubstitutionMap subs) => new(SubstitutionMap.MergeRef(subs, Substitutions));

    /// <summary>
    /// Applies all redundant substitutions and removes them from the set of returned substitutions.
    /// </summary>
    public static IEnumerable<Substitution> Simplify(SubstitutionMap subs)
    {
        var answers = new Queue<Substitution>();
        var retry = new Queue<Substitution>();
        var output = new HashSet<Substitution>();
        foreach (var s in subs
            .Where(s => s.Lhs is Variable { Ignored: false }))
            answers.Enqueue(s);
        var steps = subs
            .Where(s => s.Lhs is Variable { Ignored: true })
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
            if (sub.Lhs is null)
                continue;
            output.Remove(output.First(x => x.Lhs.Equals(ans.Lhs)));
            output.Add(new Substitution(ans.Lhs, sub.Lhs));
        }

        return output;
    }

    public ITerm this[Variable key] => Substitutions[key];

    public Solution Simplify()
    {
        return new(new(Simplify(Substitutions)
            .Where(s => s.Lhs is Variable { Ignored: false })))
            ;
    }
}
