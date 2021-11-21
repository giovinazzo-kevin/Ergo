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
                    ;
                IEnumerable<Substitution> Inner(IEnumerable<Substitution> subs)
                {
                    var answers = subs
                        .Where(s => s.Lhs.Reduce(_ => false, v => !v.Ignored, _ => false));
                    var steps = subs
                        .Where(s => s.Lhs.Reduce(_ => false, v => v.Ignored, _ => false))
                        .ToDictionary(s => s.Lhs);

                    foreach (var ans in answers) {
                        var ret = ans;
                        while(!ret.Rhs.IsGround) {
                            ret = ret.WithRhs(Term.Variables(ret.Rhs).Aggregate(ret.Rhs, (a, b) => Term.Substitute(a, steps[b])));
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
