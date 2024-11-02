﻿using Ergo.Modules;

namespace Ergo.Lang.Ast.Terms.Interfaces;

public abstract class AbstractTerm : ITerm
{
    public Maybe<ParserScope> Scope { get; }
    public abstract ITerm CanonicalForm { get; set; }
    public abstract bool IsGround { get; }
    public abstract bool IsQualified { get; }
    public abstract bool IsParenthesized { get; }
    public abstract IEnumerable<Variable> Variables { get; }
    public AbstractTerm(Maybe<ParserScope> scope)
    {
        Scope = scope;
    }

    public abstract AbstractTerm AsParenthesized(bool parenthesized);
    public abstract Signature GetSignature();
    public abstract int CompareTo(ITerm other);
    public abstract bool Equals(ITerm other);
    public abstract string Explain(bool canonical = false);
    public abstract ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ITerm Substitute(Substitution s);
    public ITerm Substitute(IEnumerable<Substitution> s) => s.Aggregate((ITerm)this, (a, b) => a.Substitute(b));
    public abstract Maybe<SubstitutionMap> Unify(ITerm other);

    public override bool Equals(object obj)
    {
        if (obj is ITerm t)
            return Equals(t);
        return base.Equals(obj);
    }

    public override int GetHashCode() => base.GetHashCode();
}
