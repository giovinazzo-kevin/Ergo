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
                    foreach (var s in subs) {
                        var ret = s;
                        int chg;
                        do {
                            chg = 0;
                            foreach (var ss in subs) {
                                if (s.Equals(ss)) continue;
                                var newRet = ret.WithRhs(Term.Substitute(ret.Rhs, ss));
                                if (!newRet.Equals(ret)) chg++;
                                ret = newRet;
                            }
                        }
                        while (chg > 0);
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
