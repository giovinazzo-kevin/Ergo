using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{
    public partial class KnowledgeBase
    {
        public readonly struct Match
        {
            public readonly Term Lhs;
            public readonly Predicate Rhs;
            public readonly Substitution[] Substitutions;

            public Match(Term lhs, Predicate rhs, IEnumerable<Substitution> substitutions)
            {
                Lhs = lhs;
                Rhs = rhs;
                Substitutions = substitutions.ToArray();
            }
        }
    }

}
