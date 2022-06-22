namespace Ergo.Lang.Ast;

public readonly struct Expansion
{
    public readonly Predicate Predicate;

    public Expansion(Predicate pred) => Predicate = pred;
}
