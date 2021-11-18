using System.Collections.Generic;

namespace Ergo.Lang
{

    public partial class Solver
    {
        public readonly struct Solution
        {
            public readonly Substitution[] Substitutions;
            public Solution(params Substitution[] subs)
            {
                Substitutions = subs;
            }
        }
    }
}
