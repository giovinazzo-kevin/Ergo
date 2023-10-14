using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(false) }")]
public class Dict : AbstractTerm
{
    protected ITerm CanonicalForm { get; }
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
            return dict.Functor.Equals(Functor)
                && Dictionary.SequenceEqual(dict.Dictionary);
        return CanonicalForm.Equals(other);
    }

    public readonly ITerm[] KeyValuePairs;
    public readonly ImmutableDictionary<Atom, ITerm> Dictionary;
    public readonly Either<Atom, Variable> Functor;
    public readonly Either<Variable, Set> Argument;

    public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<Atom, ITerm>> args = null, Maybe<ParserScope> scope = default, bool parenthesized = false)
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
        var set = new Set(KeyValuePairs, scope, false);
        CanonicalForm = new Complex(
            WellKnown.Functors.Dict.First(),
            new[] { Functor.Reduce(a => (ITerm)a, b => b),
                set
            }).AsParenthesized(parenthesized);
        IsParenthesized = parenthesized;
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
        Argument = set;
    }
    public Dict(Either<Atom, Variable> functor, Variable unboundArgs, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(scope)
    {
        Functor = functor;
        Dictionary = ImmutableDictionary.Create<Atom, ITerm>();
        var op = WellKnown.Operators.NamedArgument;
        KeyValuePairs = Array.Empty<ITerm>();
        CanonicalForm = new Complex(
            WellKnown.Functors.Dict.First(),
            new[] { Functor.Reduce(a => (ITerm)a, b => b),
                unboundArgs
            }).AsParenthesized(parenthesized);
        IsParenthesized = parenthesized;
        Signature = CanonicalForm.GetSignature();
        if (functor.IsA)
            Signature = Signature.WithTag(functor.Reduce(a => a, v => throw new InvalidOperationException()));
        Argument = unboundArgs;
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

    public override Maybe<SubstitutionMap> Unify(ITerm other)
    {
        if (other is Variable v)
        {
            var ret2 = new SubstitutionMap() { new Substitution(v, this) };
            return ret2;
        }
        if (other is not Dict dict)
            return LanguageExtensions.Unify(CanonicalForm, other);
        var map = new SubstitutionMap();
        var dxFunctor = Functor.Reduce(a => (ITerm)a, v => v);
        var dyFunctor = dict.Functor.Reduce(a => (ITerm)a, v => v);
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
            var set = Dictionary.Keys.Intersect(dict.Dictionary.Keys);
            if (!set.Any() && Dictionary.Count != 0 && dict.Dictionary.Count != 0)
                return default;
            map.AddRange(set
                .Select(key => new Substitution(Dictionary[key], dict.Dictionary[key]))
                .Prepend(new Substitution(dxFunctor, dyFunctor)));
        }
        return Maybe.Some(map);
    }

    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
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
    public override ITerm NumberVars() => CanonicalForm.NumberVars();
    public override AbstractTerm AsParenthesized(bool parenthesized) => new Dict(Functor, Dictionary, Scope, IsParenthesized);
}