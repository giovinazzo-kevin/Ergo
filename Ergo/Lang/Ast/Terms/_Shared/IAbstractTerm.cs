namespace Ergo.Lang.Ast.Terms.Interfaces;

public interface IAbstractTerm : IExplainable
{
    ITerm CanonicalForm { get; }
    Signature Signature { get; }

    Maybe<SubstitutionMap> Unify(IAbstractTerm other);
    IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    IAbstractTerm Substitute(Substitution s);
    /// Instance version of the static FromCanonical used by reflection, implemented by some abstract terms
    Maybe<IAbstractTerm> FromCanonicalTerm(ITerm c);

    string Explain();
    string IExplainable.Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.Explain(true);
        return Explain();
    }
    IAbstractTerm Substitute(IEnumerable<Substitution> subs)
    {
        var @base = this;
        foreach (var sub in subs)
        {
            @base = @base.Substitute(sub);
        }

        return @base;
    }
}
