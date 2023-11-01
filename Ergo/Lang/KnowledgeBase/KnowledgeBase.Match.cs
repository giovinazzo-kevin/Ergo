namespace Ergo.Lang;

public readonly struct KBMatch
{
    public readonly ITerm Goal;
    public readonly Predicate Predicate;
    public readonly SubstitutionMap Substitutions;

    public KBMatch(ITerm lhs, Predicate rhs, SubstitutionMap substitutions)
    {
        Goal = lhs;
        Predicate = rhs;
        Substitutions = substitutions;
    }
}
