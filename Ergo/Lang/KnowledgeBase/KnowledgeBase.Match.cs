namespace Ergo.Lang;

public readonly struct KBMatch
{
    public readonly ITerm Lhs;
    public readonly Predicate Rhs;
    public readonly SubstitutionMap Substitutions;

    public KBMatch(ITerm lhs, Predicate rhs, SubstitutionMap substitutions)
    {
        Lhs = lhs;
        Rhs = rhs;
        Substitutions = substitutions;
    }
}
