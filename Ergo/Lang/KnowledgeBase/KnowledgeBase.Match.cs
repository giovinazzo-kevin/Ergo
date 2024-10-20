namespace Ergo.Lang;

public readonly struct KBMatch
{
    public readonly ITerm Goal;
    public readonly Clause Predicate;
    public readonly SubstitutionMap Substitutions;

    public KBMatch(ITerm lhs, Clause rhs, SubstitutionMap substitutions)
    {
        Goal = lhs;
        Predicate = rhs;
        Substitutions = substitutions;
    }
}
