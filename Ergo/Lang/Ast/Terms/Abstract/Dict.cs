using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public sealed class Dict : IAbstractTerm
{
    public ITerm CanonicalForm { get; }
    public Signature Signature { get; }

    public readonly ITerm[] KeyValuePairs;
    public readonly ImmutableDictionary<Atom, ITerm> Dictionary;
    public readonly Either<Atom, Variable> Functor;

    public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<Atom, ITerm>> args = default)
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
        CanonicalForm = new Complex(WellKnown.Functors.Dict.First(), new[] { Functor.Reduce(a => (ITerm)a, b => b), new List(KeyValuePairs).CanonicalForm })
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
    }

    public Dict WithFunctor(Either<Atom, Variable> newFunctor) => new(newFunctor, Dictionary.ToBuilder());

    public string Explain(bool canonical, bool concise)
    {
        var functor = Functor.Reduce(a => a.WithAbstractForm(default).Explain(canonical), b => b.WithAbstractForm(default).Explain(canonical));
        if (false && concise)
            return $"{functor}{{{(Dictionary.Any() ? "..." : "")}}}";
        var joinedArgs = Dictionary.Join(kv =>
        {
            if (kv.Value.IsAbstract<Dict>().TryGetValue(out var inner))
                return $"{kv.Key.Explain(canonical)}: {inner.Explain(canonical, concise)}";
            return $"{kv.Key.Explain(canonical)}: {kv.Value.Explain(canonical)}";
        });
        return $"{functor}{{{joinedArgs}}}";
    }

    public string Explain() => Explain(false, true);

    public Maybe<SubstitutionMap> Unify(IAbstractTerm other)
    {
        if (other is not Dict dict)
            return CanonicalForm.Unify(other.CanonicalForm);

        var dxFunctor = Functor.Reduce(a => (ITerm)a, v => v);
        var dyFunctor = dict.Functor.Reduce(a => (ITerm)a, v => v);
        var set = Dictionary.Keys.Intersect(dict.Dictionary.Keys);
        if (!set.Any() && dict.Dictionary.Count != 0 && dict.Dictionary.Count != 0)
            return default;
        var subs = set
            .Select(key => new Substitution(Dictionary[key], dict.Dictionary[key]))
            .Prepend(new Substitution(dxFunctor, dyFunctor));
        return Maybe.Some(new SubstitutionMap(subs));
    }

    public IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        var newFunctor = Functor.Reduce(
            a => a.Instantiate(ctx, vars),
            v => v.Instantiate(ctx, vars));
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Instantiate(ctx, vars)));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        return new Dict(newFunctor is Atom a ? a : (Variable)newFunctor, newKvp);
    }

    public IAbstractTerm Substitute(Substitution s)
    {
        var newFunctor = Functor.Reduce(
            a => ((ITerm)a).Substitute(s),
            v => ((ITerm)v).Substitute(s));
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Substitute(s)));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        return new Dict(newFunctor is Atom a ? a : (Variable)newFunctor, newKvp);
    }

    public static Maybe<Dict> FromCanonical(ITerm canonical)
    {
        if (canonical is not Complex c || !WellKnown.Functors.Dict.Contains(c.Functor) || c.Arguments.Length != 2)
            return default;
        var functor = c.Arguments[0].Reduce<Either<Atom, Variable>>(a => a, v => v, c => throw new NotSupportedException());
        if (!c.Arguments[1].IsAbstract<List>().TryGetValue(out var list))
            return default;
        if (!list.Contents.All(x => x is Complex d && WellKnown.Functors.NamedArgument.Contains(d.Functor) && d.Arguments.Length == 2 && d.Arguments[0] is Atom))
            return default;
        var args = list.Contents.Cast<Complex>().Select(c => new KeyValuePair<Atom, ITerm>((Atom)c.Arguments[0], c.Arguments[1]));
        return Maybe.Some<Dict>(new(functor, args));
    }
    Maybe<IAbstractTerm> IAbstractTerm.FromCanonicalTerm(ITerm canonical) => FromCanonical(canonical).Select(x => (IAbstractTerm)x);
}