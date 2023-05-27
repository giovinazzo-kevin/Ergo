namespace Ergo.Lang.Ast;

public readonly record struct ExpansionResult(ITerm Match, NTuple Expansion, Maybe<Variable> Binding)
{
    public ExpansionResult Substitute(SubstitutionMap subs) => new(Match, (NTuple)Expansion.Substitute(subs), Binding);
}
