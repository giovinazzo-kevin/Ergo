
using System;
using System.Linq;

namespace Ergo.Lang
{

    public readonly partial struct Complex
    {
        public readonly Atom Functor { get; }
        public readonly Term[] Arguments { get; }
        public readonly int Arity => Arguments.Length;

        public static string Explain(Complex c)
        {
            if (CommaExpression.TryUnfold(c, out var comma)) {
                return Sequence.Explain(comma.Sequence);
            }
            if (List.TryUnfold(c, out var list)) {
                return Sequence.Explain(list.Sequence);
            }
            return $"{c.Functor}({String.Join(", ", c.Arguments)})";
        }

        public Complex(Atom functor, params Term[] args)
        {
            Functor = functor;
            Arguments = args;
        }

        public Complex WithFunctor(Atom functor)
        {
            return new Complex(functor, Arguments);
        }

        public Complex WithArguments(params Term[] args)
        {
            if (args.Length != Arguments.Length)
                throw new ArgumentOutOfRangeException();
            return new Complex(Functor, args);
        }

        public bool Matches(Complex other)
        {
            return Equals(Functor, other.Functor) && Arity == other.Arity;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Complex other)) {
                return false;
            }
            var args = Arguments;
            return Matches(other) && Enumerable.Range(0, Arity).All(i => Equals(args[i], other.Arguments[i]));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Functor, Arity);
        }

        public static implicit operator Term(Complex rhs)
        {
            return Term.FromComplex(rhs);
        }

        public override string ToString()
        {
            return Explain(this);
        }
    }

}
