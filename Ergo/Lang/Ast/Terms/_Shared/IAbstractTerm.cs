namespace Ergo.Lang.Ast.Terms.Interfaces;

public interface IAbstractTerm : IExplainable
{
    ITerm CanonicalForm { get; }
    Signature Signature { get; }

    Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other);
    string Explain();
    string IExplainable.Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.Explain();
        return Explain();
    }
}
