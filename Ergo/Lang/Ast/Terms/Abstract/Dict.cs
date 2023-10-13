using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public class Dict : AbstractTerm
{
    protected ITerm CanonicalForm { get; }
    public Signature Signature { get; }
    public override bool IsQualified => CanonicalForm.IsQualified;
    public override bool IsParenthesized { get; }
    public override bool IsGround => CanonicalForm.IsGround;
    public override IEnumerable<Variable> Variables => CanonicalForm.Variables;
    public override int CompareTo(ITerm other) => CanonicalForm.CompareTo(other);
    public override bool Equals(ITerm other) => CanonicalForm.Equals(other);

    public readonly ITerm[] KeyValuePairs;
    public readonly ImmutableDictionary<Atom, ITerm> Dictionary;
    public readonly Either<Atom, Variable> Functor;

    public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<Atom, ITerm>> args, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(scope)
    {
        args ??= Enumerable.Empty<KeyValuePair<Atom, ITerm>>();
        Functor = functor;
        Dictionary = ImmutableDictionary.CreateRange(args);
        var op = WellKnown.Operators.NamedArgument;
        KeyValuePairs = Dictionary
            .Select(kv => (ITerm)new Complex(op.CanonicalFunctor, kv.Key, kv.Value)
                    .AsOperator(op))
            .OrderBy(o => o)
            .ToArray();
        CanonicalForm = new Complex(
            WellKnown.Functors.Dict.First(),
            new[] { Functor.Reduce(a => (ITerm)a, b => b),
                new Set(KeyValuePairs, scope, false)
            }).AsParenthesized(parenthesized);
        IsParenthesized = parenthesized;
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
    }

    public Dict WithFunctor(Either<Atom, Variable> newFunctor) => new(newFunctor, Dictionary.ToBuilder(), Scope, IsParenthesized);

    public override string Explain(bool canonical)
    {
        var functor = Functor.Reduce(a => a.Explain(canonical), b => b.Explain(canonical));
        var joinedArgs = Dictionary.Join(kv =>
        {
            if (kv.Value is Dict inner)
                return $"{kv.Key.Explain(canonical)}: {inner.Explain(canonical)}";
            return $"{kv.Key.Explain(canonical)}: {kv.Value.Explain(canonical)}";
        });
        return $"{functor}{{{joinedArgs}}}";
    }

    public override Maybe<SubstitutionMap> UnifyLeftToRight(ITerm other)
    {
        if (other is not Dict dict)
            return CanonicalForm.Unify(other);
        var dxFunctor = Functor.Reduce(a => (ITerm)a, v => v);
        var dyFunctor = dict.Functor.Reduce(a => (ITerm)a, v => v);
        var set = Dictionary.Keys.Intersect(dict.Dictionary.Keys);
        if (!set.Any() && Dictionary.Count != 0 && dict.Dictionary.Count != 0)
            return default;
        var subs = set
            .Select(key => new Substitution(Dictionary[key], dict.Dictionary[key]))
            .Prepend(new Substitution(dxFunctor, dyFunctor));
        return Maybe.Some(new SubstitutionMap(subs));
    }

    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        var newFunctor = Functor.Reduce(
            a => a.Instantiate(ctx, vars),
            v => v.Instantiate(ctx, vars));
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Instantiate(ctx, vars)));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        return new Dict(newFunctor is Atom a ? a : (Variable)newFunctor, newKvp, Scope, IsParenthesized);
    }

    public override AbstractTerm Substitute(Substitution s)
    {
        var newFunctor = Functor.Reduce(
            a => ((ITerm)a).Substitute(s),
            v => ((ITerm)v).Substitute(s));
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Substitute(s)));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        return new Dict(newFunctor is Atom a ? a : (Variable)newFunctor, newKvp, Scope, IsParenthesized);
    }

    public override Signature GetSignature() => CanonicalForm.GetSignature();
    public override ITerm NumberVars() => CanonicalForm.NumberVars();
    public override AbstractTerm AsParenthesized(bool parenthesized) => new Dict(Functor, Dictionary, Scope, IsParenthesized);
}