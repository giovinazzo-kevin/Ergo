﻿using System.Collections.Immutable;

namespace Ergo.Solver;

public readonly struct Solution
{
    public readonly Substitution[] Substitutions;
    public readonly Lazy<ImmutableDictionary<ITerm, ITerm>> Links;

    /// <summary>
    /// Applies all redundant substitutions and removes them from the set of returned substitutions.
    /// </summary>
    public Solution Simplify()
    {
        return new(Inner(Substitutions)
            .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false, d => false))
            .ToArray())
            ;
        IEnumerable<Substitution> Inner(IEnumerable<Substitution> subs)
        {
            var answers = subs
                .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false, d => false));
            var steps = subs
                .Where(s => s.Lhs.Reduce(_ => false, v => v.Ignored, _ => false, d => false))
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
                    {
                        break;
                    }

                    vars = newVars;
                }

                yield return ret;
            }
        }
    }
    public Solution(params Substitution[] subs)
    {
        Substitutions = subs;
        Links = new(() => ImmutableDictionary<ITerm, ITerm>.Empty
            .AddRange(subs.Select(s => new KeyValuePair<ITerm, ITerm>(s.Lhs, s.Rhs))), true);
    }
}
