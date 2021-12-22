using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
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
                        var vars = ret.Rhs.Variables.ToArray();
                        while (!ret.Rhs.IsGround) {
                            ret = ret.WithRhs(vars.Aggregate(ret.Rhs, (a, b) => steps.ContainsKey(b) ? a.Substitute(steps[b]) : a));
                            var newVars = ret.Rhs.Variables.ToArray();
                            if(newVars.Where(v => vars.Contains(v)).Any()) {
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
            }
        }
    }
}
