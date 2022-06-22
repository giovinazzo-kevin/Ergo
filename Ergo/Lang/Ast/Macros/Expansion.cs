namespace Ergo.Lang.Ast;

public readonly struct Expansion
{
    public readonly Variable OutputVariable;
    public readonly Predicate Predicate;

    public Expansion(Variable outVar, Predicate pred)
    {
        Predicate = pred;
        OutputVariable = outVar;
    }
}
