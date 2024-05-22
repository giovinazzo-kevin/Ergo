using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Compiler;
using Ergo.Lang.Parser;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(false) }")]
public class Dict : AbstractTerm
{
    private static readonly DictCompiler DictCompiler = new();
    public override IAbstractTermCompiler Compiler => DictCompiler;
    public override ITerm CanonicalForm { get; set; }
    public Signature Signature { get; }
    public override bool IsQualified => CanonicalForm.IsQualified;
    public override bool IsParenthesized { get; }
    public override bool IsGround => CanonicalForm.IsGround;
    public override IEnumerable<Variable> Variables => CanonicalForm.Variables;
    public override int CompareTo(ITerm other) => CanonicalForm.CompareTo(other);
    public override int GetHashCode() => CanonicalForm.GetHashCode();
    public override bool Equals(ITerm other)
    {
        if (other is Dict dict)
        {
            if (dict.Dictionary.Count != Dictionary.Count)
                return false;
            if (!dict.Functor.Equals(Functor))
                return false;
            // Defer value comparisons, because they are potentially much more expensive than comparing atomic keys.
            var valueComparisons = new Queue<(ITerm, ITerm)>();
            foreach (var (keyA, valueA) in Dictionary)
            {
                if (!dict.Dictionary.TryGetValue(keyA, out var valueB))
                    return false;
                valueComparisons.Enqueue((valueA, valueB));
            }
            while (valueComparisons.TryDequeue(out var pair))
            {
                if (!pair.Item1.Equals(pair.Item2))
                    return false;
            }
            return true;
        }
        return CanonicalForm.Equals(other);
    }

    public ITerm[] KeyValuePairs { get; protected set; }
    public ImmutableDictionary<Atom, ITerm> Dictionary { get; protected set; }
    public Either<Atom, Variable> Functor { get; protected set; }
    public Either<Variable, Set> Argument { get; protected set; }

    protected ITerm[] BuildKVPs()
    {
        var op = WellKnown.Operators.NamedArgument;
        return Dictionary
            .Select(kv => (ITerm)new Complex(op.CanonicalFunctor, kv.Key, kv.Value)
                    .AsOperator(op))
            .OrderBy(o => o)
            .ToArray();
    }

    protected ITerm BuildCanonical()
    {
        return new Complex(
            WellKnown.Functors.Dict.First(),
            new[] { Functor.Reduce(a => (ITerm)a, b => b),
                Argument.Reduce<ITerm>(x => x, x => x)
            }).AsParenthesized(IsParenthesized);
    }

    public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<Atom, ITerm>> args = null, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(scope)
    {
        args ??= Enumerable.Empty<KeyValuePair<Atom, ITerm>>();
        Functor = functor;
        Dictionary = ImmutableDictionary.CreateRange(args);
        KeyValuePairs = BuildKVPs();
        Argument = new Set(KeyValuePairs, scope, false);
        IsParenthesized = parenthesized;
        CanonicalForm = BuildCanonical();
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
    }
    public Dict(Either<Atom, Variable> functor, Variable unboundArgs, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(scope)
    {
        Functor = functor;
        Dictionary = ImmutableDictionary.Create<Atom, ITerm>();
        var op = WellKnown.Operators.NamedArgument;
        KeyValuePairs = Array.Empty<ITerm>();
        Argument = unboundArgs;
        IsParenthesized = parenthesized;
        CanonicalForm = BuildCanonical();
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
    }

    public Dict WithFunctor(Either<Atom, Variable> newFunctor) => new(newFunctor, Dictionary.ToBuilder(), Scope, IsParenthesized);

    public override string Explain(bool canonical)
    {
        var functor = Functor.Reduce(a => a.Explain(canonical), b => b.Explain(canonical));
        if (Argument.TryGetA(out var var))
        {
            return $"dict({functor}, {var.Explain(canonical)})";
        }
        var joinedArgs = Dictionary.Join(kv =>
        {
            if (kv.Value is Dict inner)
                return $"{kv.Key.Explain(canonical)}: {inner.Explain(canonical)}";
            return $"{kv.Key.Explain(canonical)}: {kv.Value.Explain(canonical)}";
        });
        return $"{functor}{{{joinedArgs}}}";
    }

    public Dict Merge(Dict other)
    {
        Either<Atom, Variable> functor = Functor.TryGetA(out var a)
            ? a
            : other.Functor.TryGetA(out a)
            ? a
            : WellKnown.Literals.Discard;
        var kvps = Dictionary.Concat(other.Dictionary)
            .DistinctBy(x => x.Key);
        return new Dict(functor, kvps);
    }

    public override Maybe<SubstitutionMap> Unify(ITerm other)
    {
        var map = SubstitutionMap.Pool.Acquire();
        if (other is Variable v)
        {
            map.Add(new Substitution(v, this));
            return map;
        }
        if (other is not Dict dict)
            return LanguageExtensions.Unify(CanonicalForm, other);
        var dxFunctor = Functor.Reduce(a => (ITerm)a, v => v);
        var dyFunctor = dict.Functor.Reduce(a => (ITerm)a, v => v);
        if (!dxFunctor.Unify(dyFunctor).TryGetValue(out var subs))
            return default;
        map.AddRange(subs);
        // complication: the form dict(_, Variable) should be parsed, but
        // in that case Variable should unify with the whole set of KVPs
        if (Argument.TryGetA(out var av))
        {
            map.Add(new(av, dict.Argument.Reduce<ITerm>(x => x, x => x)));
        }
        if (dict.Argument.TryGetA(out var bv))
        {
            map.Add(new(bv, Argument.Reduce<ITerm>(x => x, x => x)));
        }
        else
        {
            // Proper dictionaries unify if the intersection of their properties unifies.
            // This is different from equality, which is more strict and checks if all elements are equal.
            SubstitutionMap.Pool.Release(subs);
            if (Dictionary.Count == 0 || dict.Dictionary.Count == 0)
                return map;
            var keys = new HashSet<Atom>(Dictionary.Keys);
            keys.IntersectWith(dict.Dictionary.Keys);
            if (keys.Count == 0)
                return default;
            foreach (var key in keys)
            {
                if (!Dictionary[key].Unify(dict.Dictionary[key]).TryGetValue(out subs))
                    return default;
                map.AddRange(subs);
                SubstitutionMap.Pool.Release(subs);
            }
        }
        return Maybe.Some(map);
    }


    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround)
            return this;
        vars ??= new();
        var newFunctor = Functor.Reduce(
            a => a.Instantiate(ctx, vars),
            v => v.Instantiate(ctx, vars));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        var either = (Either<Atom, Variable>)(newFunctor is Atom ? (Atom)newFunctor : (Variable)newFunctor);
        if (Argument.TryGetA(out var var))
        {
            var newVar = (Variable)var.Instantiate(ctx, vars);
            return new Dict(either, newVar, Scope, IsParenthesized);
        }
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Instantiate(ctx, vars)));
        return new Dict(either, newKvp, Scope, IsParenthesized);
    }

    public override AbstractTerm Substitute(Substitution s)
    {
        if (IsGround)
            return this;
        var newFunctor = Functor.Reduce(
            a => ((ITerm)a).Substitute(s),
            v => ((ITerm)v).Substitute(s));
        var either = (Either<Atom, Variable>)(newFunctor is Atom ? (Atom)newFunctor : (Variable)newFunctor);
        if (Argument.TryGetA(out var var))
        {
            var newArg = var.Substitute(s);
            if (newArg is Set set)
            {
                var pairs = DictParser.GetPairs(set, set.Scope.GetOr(default));
                return new Dict(either, pairs, Scope, IsParenthesized);
            }
            if (newArg is Variable newVar)
            {
                return new Dict(either, newVar, Scope, IsParenthesized);
            }
            throw new InvalidOperationException();
        }
        var newKvp = Dictionary.Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key, kvp.Value.Substitute(s)));
        if (newFunctor is not Variable and not Atom)
            throw new InvalidOperationException();
        return new Dict(either, newKvp, Scope, IsParenthesized);
    }

    public override Signature GetSignature() => CanonicalForm.GetSignature();
    public override AbstractTerm AsParenthesized(bool parenthesized) => new Dict(Functor, Dictionary, Scope, IsParenthesized);
}