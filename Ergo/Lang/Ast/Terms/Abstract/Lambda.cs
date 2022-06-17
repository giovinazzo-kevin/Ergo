using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class Lambda : IAbstractTerm
{
    public ITerm CanonicalForm { get; }
    public Signature Signature { get; }

    public readonly Set FreeVars;
    public readonly List BoundVars;
    public readonly ITerm Goal;

    public Lambda(Set freeVars, List boundVars, ITerm goal)
    {
        FreeVars = freeVars;
        BoundVars = boundVars;
        Goal = goal;
        CanonicalForm = new Complex(WellKnown.Functors.Lambda.First(), new Complex(WellKnown.Functors.Division.First(), FreeVars.CanonicalForm, BoundVars.CanonicalForm), Goal)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
        Signature = CanonicalForm.GetSignature();
    }
    public Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other)
        => CanonicalForm.WithAbstractForm(default).Unify(other.CanonicalForm.WithAbstractForm(default));
    public string Explain()
    {
        var bound = $"{BoundVars.Explain()}{WellKnown.Functors.Lambda.First().Explain()}{Goal.Explain()}";
        if (FreeVars.IsEmpty)
            return bound;
        return $"{FreeVars.Explain()}{WellKnown.Functors.Division.First().Explain()}{bound}";
    }

    public IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        var ret = new Lambda(FreeVars, (List)BoundVars.Instantiate(ctx, vars), Goal.Instantiate(ctx, vars));
        return ret;
    }
    public IAbstractTerm Substitute(Substitution s)
        => new Lambda((Set)FreeVars.Substitute(s), (List)BoundVars.Substitute(s), Goal.Substitute(s));
}