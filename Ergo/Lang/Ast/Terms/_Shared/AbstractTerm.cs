
namespace Ergo.Lang.Ast.Terms.Interfaces;

public abstract class AbstractTerm : ITerm
{
    public Maybe<ParserScope> Scope { get; }
    public abstract bool IsGround { get; }
    public abstract bool IsQualified { get; }
    public abstract bool IsParenthesized { get; }
    public abstract IEnumerable<Variable> Variables { get; }
    public AbstractTerm(Maybe<ParserScope> scope)
    {
        Scope = scope;
    }
    public abstract Signature GetSignature();
    public abstract int CompareTo(ITerm other);
    public abstract bool Equals(ITerm other);
    public abstract string Explain(bool canonical = false);
    public abstract ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ITerm Substitute(Substitution s);
    public abstract ITerm NumberVars();
    public ITerm Substitute(IEnumerable<Substitution> s) => s.Aggregate((ITerm)this, (a, b) => a.Substitute(b));
    public abstract Maybe<SubstitutionMap> UnifyLeftToRight(ITerm other);
}
