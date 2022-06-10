using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly partial struct Dict : ITerm
    {
        public readonly Complex CanonicalForm;

        public readonly ITerm[] KeyValuePairs;
        public readonly ImmutableDictionary<ITerm, ITerm> Dictionary;
        public readonly Either<Atom, Variable> Functor;

        private readonly int HashCode;

        public Dict(Either<Atom, Variable> functor, IEnumerable<KeyValuePair<ITerm, ITerm>> args)
        {
            Functor = functor;
            Dictionary = ImmutableDictionary.CreateRange(args);
            KeyValuePairs = args
                .Select(kv => kv.Key is Variable && kv.Value == kv.Key
                    ? kv.Key
                    : new Complex(WellKnown.Functors.NamedArgument.First(), kv.Key, kv.Value)
                        .AsOperator(OperatorAffix.Infix))
                .OrderBy(o => o)
                .ToArray();
            HashCode = KeyValuePairs.Aggregate(Functor.GetHashCode(), (hash, a) => System.HashCode.Combine(hash, a));
            IsGround = Functor.IsA ? KeyValuePairs.All(x => x.IsGround) : false;
            IsQualified = false;
            IsParenthesized = false;
            CanonicalForm = new Complex(WellKnown.Functors.Dict.First(), KeyValuePairs.Prepend(Functor.Reduce(a => (ITerm)a, b => b)).ToArray());
        }

        public bool IsGround { get; }
        public bool IsQualified { get; }
        public bool IsParenthesized { get; }
        public IEnumerable<Variable> Variables =>
            Functor.IsA ? KeyValuePairs.SelectMany(x => x.Variables)
                        : KeyValuePairs.SelectMany(x => x.Variables).Prepend(Functor.Reduce(_ => default, v => v));

        public int CompareTo(ITerm o)
        {
            if (o is Atom) return 1;
            if (o is Variable) return 1;
            if (o is Dict dict) return CompareTo(dict.CanonicalForm);
            if (o is not Complex other) throw new InvalidCastException();
            return CanonicalForm.CompareTo(other);
        }

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
            var joinedArgs = String.Join(",", KeyValuePairs.Select(kv => kv.Explain(false)));
            return $"{functor}{{{joinedArgs}}}";
        }

        public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
        {
            return new Dict(Functor.Map(a => a, b => (Variable)b.Instantiate(ctx, vars)), Dictionary
                .Select(kv => new KeyValuePair<ITerm, ITerm>(kv.Key.Instantiate(ctx, vars), kv.Value.Instantiate(ctx, vars))));
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
                .Select(kv => new KeyValuePair<ITerm, ITerm>(kv.Key.Substitute(s), kv.Value.Substitute(s)));
            return new Dict(newFunctor, newArgs);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public static bool operator ==(Dict left, Dict right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Dict left, Dict right)
        {
            return !(left == right);
        }
    }
}
