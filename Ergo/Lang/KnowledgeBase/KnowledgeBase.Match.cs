namespace Ergo.Lang;

public readonly struct KBMatch(ITerm lhs, Predicate rhs, SubstitutionMap substitutions)
{
    public readonly ITerm Goal = lhs;
    public readonly Predicate Predicate = rhs;
    public readonly SubstitutionMap Substitutions = substitutions;
}
