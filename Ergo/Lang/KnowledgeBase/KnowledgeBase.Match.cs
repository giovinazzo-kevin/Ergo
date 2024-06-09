using Ergo.Lang.Compiler;

namespace Ergo.Lang;

public readonly struct KBMatch(ITerm lhs, Predicate rhs, SubstitutionMap substitutions)
{
    public readonly ITerm Goal = lhs;
    public readonly Predicate Predicate = rhs;
    public readonly SubstitutionMap Substitutions = substitutions;
}

public readonly record struct KBMatch2(ITermAddress Lhs, ErgoVM.Op Rhs);
