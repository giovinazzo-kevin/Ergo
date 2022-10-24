namespace Ergo.Solver;

public readonly struct Solution
{
    public readonly SolverScope Scope;
    public readonly Substitution[] Substitutions;
    public readonly Lazy<ImmutableDictionary<ITerm, ITerm>> Links;

    public readonly bool TailRecursiveCall;

    public static Solution Success(SolverScope scope, params Substitution[] subs) => new(scope, false, subs);

    /// <summary>
    /// Applies all redundant substitutions and removes them from the set of returned substitutions.
    /// </summary>
    public Solution Simplify()
    {
        return new(Scope, TailRecursiveCall, Inner(Substitutions)
            .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false))
            .ToArray())
            ;
        IEnumerable<Substitution> Inner(IEnumerable<Substitution> subs)
        {
            var answers = new Queue<Substitution>();
            var retry = new Queue<Substitution>();
            var output = new HashSet<Substitution>();
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
    }

    public Solution TailRecursive(bool recursive = true) => new(Scope, recursive, Substitutions);

    private Solution(SolverScope scope, bool tailRecursive, params Substitution[] subs)
    {
        Scope = scope;
        Substitutions = subs;
        TailRecursiveCall = tailRecursive;
        Links = new(() => ImmutableDictionary<ITerm, ITerm>.Empty
            .AddRange(subs.Select(s => new KeyValuePair<ITerm, ITerm>(s.Lhs, s.Rhs))), true);
    }
}
