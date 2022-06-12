namespace Ergo.Lang;

public partial class KnowledgeBase
{
    public readonly struct Match
    {
        public readonly ITerm Lhs;
        public readonly Predicate Rhs;
        public readonly Substitution[] Substitutions;

        public Match(ITerm lhs, Predicate rhs, IEnumerable<Substitution> substitutions)
        {
            Lhs = lhs;
            Rhs = rhs;
            Substitutions = substitutions.ToArray();
        }
    }
}

