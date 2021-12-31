
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly partial struct Complex : ITerm
    {
        public bool IsGround => Arguments.All(arg => arg.IsGround);
        public readonly bool IsQualified { get; }

        public readonly Atom Functor;
        public readonly ITerm[] Arguments;
        public readonly int Arity => Arguments.Length;

        private readonly int HashCode;

        public static Complex OfArity(Atom functor, int arity) => 
            new(functor, Enumerable.Range(0, arity).Select(i => (ITerm)new Variable($"_{i}")).ToArray());

        public Complex(Atom functor, params ITerm[] args)
        {
            Functor = functor;
            Arguments = args;
            HashCode = System.HashCode.Combine(Functor, Arguments.Length);
            IsQualified = args.Length == 2 && ":".Equals(functor.Value);
        }

        public string Explain()
        {
            if (CommaSequence.TryUnfold(this, out var comma)) {
                return comma.Explain();
            }
            if (List.TryUnfold(this, out var list)) {
                return list.Explain();
            }
            // TODO: remove this crap
            //if(Functor.Value.Equals("∨") && Arguments.Length == 2)
            //{
            //    return $"{Arguments[0].Explain()} ∨ {Arguments[1].Explain()}";
            //}
            return $"{Functor.Explain()}({String.Join(", ", Arguments.Select(arg => arg.Explain()))})";
        }

        public ITerm Substitute(Substitution s)
        {
            if (Equals(s.Lhs))
            {
                return s.Rhs;
            }
            var newArgs = new ITerm[Arguments.Length];
            for (int i = 0; i < newArgs.Length; i++)
            {
                newArgs[i] = Arguments[i].Substitute(s);
            }
            return WithArguments(newArgs);
        }

        public IEnumerable<Variable> Variables => Arguments.SelectMany(arg => arg.Variables);

        public Complex WithFunctor(Atom functor)
        {
            return new Complex(functor, Arguments);
        }

        public Complex WithArguments(params ITerm[] args)
        {
            if (args.Length != Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(args));
            return new Complex(Functor, args);
        }

        public bool Matches(Complex other)
        {
            return Equals(Functor, other.Functor) && Arity == other.Arity;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Complex other) {
                return false;
            }
            var args = Arguments;
            return Matches(other) && Enumerable.Range(0, Arity).All(i => Equals(args[i], other.Arguments[i]));
        }
        public bool Equals(ITerm obj) => Equals((object)obj);

        public override int GetHashCode()
        {
            return HashCode;
        }

        public int CompareTo(ITerm o)
        {
            if (o is Atom) return 1;
            if (o is Variable) return 1;
            if (o is not Complex other) throw new InvalidCastException();

            if (Arity.CompareTo(other.Arity) is var cmpArity && cmpArity != 0)
                return cmpArity;
            if (Functor.CompareTo(other.Functor) is var cmpFunctor && cmpFunctor != 0)
                return cmpFunctor;
            return Arguments.Select((a, i) => a.CompareTo(other.Arguments[i]))
                .DefaultIfEmpty(0)
                .FirstOrDefault(cmp => cmp != 0);
        }

        public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
        {
            return new Complex(Functor, Arguments.Select(arg => arg.Instantiate(ctx, vars)).ToArray());
        }

        //public ITerm Qualify(Atom m)
        //{
        //    return new Complex(new Atom($"{m.Explain()}:{Functor.Explain()}"), Arguments);
        //}

        public static bool operator ==(Complex left, Complex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Complex left, Complex right)
        {
            return !(left == right);
        }
    }

}
