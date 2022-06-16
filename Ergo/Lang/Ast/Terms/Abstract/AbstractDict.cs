using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast.Terms.Abstract;

[DebuggerDisplay("{ Explain() }")]
public sealed class Dict : IAbstractTerm
{
    public Complex CanonicalForm { get; }
    public Signature Signature { get; }

    public readonly ITerm[] KeyValuePairs;
    public readonly ImmutableDictionary<Atom, ITerm> Dictionary;
    public readonly Either<Atom, Variable> Functor;

    public static bool TryUnfold(ITerm term, out Dict dict)
    {
        dict = default;
        if (term is not Complex cplx || !WellKnown.Functors.Dict.Contains(cplx.Functor) || cplx.Arity != 2)
            return false;
        var tag = cplx.Arguments[0].Reduce<Either<Atom, Variable>>(a => a, v => v, c => throw new InvalidOperationException());
        if (!List.TryUnfold(cplx.Arguments[1], out var kvp))
            return false;
        dict = new(tag, kvp.Contents.Cast<Complex>().Select(i => new KeyValuePair<Atom, ITerm>((Atom)i.Arguments[0], i.Arguments[1])));
        return true;
    }

    public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<Atom, ITerm>> args = default)
    {
        args ??= Enumerable.Empty<KeyValuePair<Atom, ITerm>>();
        Functor = functor;
        Dictionary = ImmutableDictionary.CreateRange(args);
        KeyValuePairs = Dictionary
            .Select(kv => (ITerm)new Complex(WellKnown.Functors.NamedArgument.First(), kv.Key, kv.Value)
                    .AsOperator(OperatorAffix.Infix))
            .OrderBy(o => o)
            .ToArray();
        CanonicalForm = new Complex(WellKnown.Functors.Dict.First(), new[] { Functor.Reduce(a => (ITerm)a, b => b), new List(KeyValuePairs).Root })
            .AsAbstract(Maybe.Some<IAbstractTerm>(this));
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => Maybe.Some(a), v => throw new InvalidOperationException()));
    }

    public Dict WithFunctor(Either<Atom, Variable> newFunctor) => new(newFunctor, Dictionary.ToBuilder());

    public string Explain()
    {
        var functor = Functor.Reduce(a => a.Explain(false), b => b.Explain(false));
        var joinedArgs = string.Join(",", KeyValuePairs.Select(kv => kv.Explain(false)));
        return $"{functor}{{{joinedArgs}}}";
    }

    public Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other)
    {
        if (other is not Dict dict)
            return default;

        var dxFunctor = Functor.Reduce(a => (ITerm)a, v => v);
        var dyFunctor = dict.Functor.Reduce(a => (ITerm)a, v => v);
        var set = Dictionary.Keys.Intersect(dict.Dictionary.Keys);
        if (!set.Any() && dict.Dictionary.Count != 0 && dict.Dictionary.Count != 0)
            return default;
        return Maybe.Some(set
            .Select(key => new Substitution(Dictionary[key], dict.Dictionary[key]))
            .Prepend(new Substitution(dxFunctor, dyFunctor)));
    }
}