using System.Collections.Immutable;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly partial struct Dict : ITerm
{
    public readonly Complex CanonicalForm;

    public readonly ITerm[] KeyValuePairs;
    public readonly ImmutableDictionary<Atom, ITerm> Dictionary;
    public readonly Either<Atom, Variable> Functor;

    private readonly int HashCode;

    public static bool TryUnfold(ITerm term, out Dict dict)
    {
        dict = default;
        if (term is not Complex cplx || !WellKnown.Functors.Dict.Contains(cplx.Functor) || cplx.Arity != 2)
            return false;
        var tag = cplx.Arguments[0].Reduce<Either<Atom, Variable>>(a => a, v => v, c => throw new InvalidOperationException(), d => throw new InvalidOperationException());
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
        IsGround = Functor.IsA && KeyValuePairs.All(x => x.IsGround);
        IsQualified = false;
        IsParenthesized = false;
        CanonicalForm = new Complex(WellKnown.Functors.Dict.First(), new[] { Functor.Reduce(a => (ITerm)a, b => b), new List(KeyValuePairs).Root });
        HashCode = CanonicalForm.GetHashCode();
    }
    private Dict(Either<Atom, Variable> functor, ImmutableDictionary<Atom, ITerm> dict, ITerm[] kvp)
    {
        Functor = functor;
        Dictionary = dict;
        KeyValuePairs = kvp;
        IsGround = Functor.IsA && KeyValuePairs.All(x => x.IsGround);
        IsQualified = false;
        IsParenthesized = false;
        CanonicalForm = new Complex(WellKnown.Functors.Dict.First(), new[] { Functor.Reduce(a => (ITerm)a, b => b), new List(KeyValuePairs).Root });
        HashCode = CanonicalForm.GetHashCode();
    }

    public bool IsGround { get; }
    public bool IsQualified { get; }
    public bool IsParenthesized { get; }
    public IEnumerable<Variable> Variables =>
        Functor.IsA ? Dictionary.Values.SelectMany(x => x.Variables)
                    : Dictionary.Values.SelectMany(x => x.Variables).Prepend(Functor.Reduce(_ => default, v => v));
    public int CompareTo(ITerm o)
    {
        if (o is Atom) return 1;
        if (o is Variable) return 1;
        if (o is Dict dict) return CompareTo(dict.CanonicalForm);
        if (o is not Complex other) throw new InvalidCastException();
        return CanonicalForm.CompareTo(other);
    }

    public Dict WithFunctor(Either<Atom, Variable> newFunctor) => new(newFunctor, Dictionary, KeyValuePairs);

    public override bool Equals(object obj)
    {
        var canonical = obj switch
        {
            Complex c => Maybe.Some(c),
            Dict d => Maybe.Some(d.CanonicalForm),
            _ => Maybe.None<Complex>()
        };
        if (!canonical.HasValue)
            return false;
        return CanonicalForm.Equals(canonical.GetOrThrow());
    }
    public bool Equals(ITerm obj) => Equals((object)obj);

    public string Explain(bool canonical = false)
    {
        if (canonical)
            return CanonicalForm.Explain(true);
        var functor = Functor.Reduce(a => a.Explain(false), b => b.Explain(false));
        var joinedArgs = string.Join(",", KeyValuePairs.Select(kv => kv.Explain(false)));
        return $"{functor}{{{joinedArgs}}}";
    }

    public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new Dict(Functor.Map(a => a, b => (Variable)b.Instantiate(ctx, vars)), Dictionary
            .Select(kv => new KeyValuePair<Atom, ITerm>(kv.Key, kv.Value.Instantiate(ctx, vars))));
    }

    public ITerm Substitute(Substitution s)
    {
        if (Equals(s.Lhs))
        {
            return s.Rhs;
        }

        var functor = Functor.Reduce(a => a, v => v.Substitute(s));
        var newFunctor = functor switch
        {
            Atom a => (Either<Atom, Variable>)a,
            Variable v => (Either<Atom, Variable>)v,
            _ => throw new InvalidOperationException()
        };
        var newArgs = Dictionary
            .Select(kv => new KeyValuePair<Atom, ITerm>(kv.Key, kv.Value.Substitute(s)));
        return new Dict(newFunctor, newArgs);
    }

    public override int GetHashCode() => HashCode;

    public static bool operator ==(Dict left, Dict right) => left.Equals(right);

    public static bool operator !=(Dict left, Dict right) => !(left == right);
}
