namespace Ergo.Lang.Ast.Terms.Interfaces;

public interface IAbstractTerm : IExplainable
{
    ITerm CanonicalForm { get; }
    Signature Signature { get; }

    Maybe<SubstitutionMap> Unify(IAbstractTerm other);
    IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    IAbstractTerm Substitute(Substitution s);

    string Explain();
    string IExplainable.Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.WithAbstractForm(default).Explain(true);
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
