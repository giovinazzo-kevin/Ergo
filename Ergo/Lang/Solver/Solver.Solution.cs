using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{

    public partial class Solver
    {
        public readonly struct Solution
        {
            public readonly Substitution[] Substitutions;
            /// <summary>
            /// Applies all redundant substitutions and removes them from the set of returned substitutions.
            /// </summary>
            public IEnumerable<Substitution> Simplify()
            {
                return Inner(Substitutions)
                    .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false))
                    .OrderBy(s => s.Lhs.ToString());
                IEnumerable<Substitution> Inner(IEnumerable<Substitution> subs)
                {
                    subs = subs.OrderByDescending(s => s.Lhs.ToString());
                    foreach (var s in subs) {
                        var ret = s;
                        foreach (var ss in subs) {
                            if (s.Equals(ss)) continue;
                            ret = ret.WithRhs(Term.Substitute(ret.Rhs, ss));
                        }
                        yield return ret;
                    }
                }
            }
            public Solution(params Substitution[] subs)
            {
                Substitutions = subs;
            }
        }
    }
}
