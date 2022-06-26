namespace Ergo.Lang;

public readonly struct KBMatch
{
    public readonly ITerm Lhs;
    public readonly Predicate Rhs;
    public readonly Substitution[] Substitutions;

    public KBMatch(ITerm lhs, Predicate rhs, IEnumerable<Substitution> substitutions)
    {
        Lhs = lhs;
        Rhs = rhs;
        Substitutions = substitutions.ToArray();
    }
}
