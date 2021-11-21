
using System;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly partial struct Complex
    {
        public readonly Atom Functor;
        public readonly Term[] Arguments;
        public readonly int Arity => Arguments.Length;

        public static string Explain(Complex c)
        {
            if (CommaExpression.TryUnfold(c, out var comma)) {
                return CommaExpression.Explain(comma);
            }
            if (List.TryUnfold(c, out var list)) {
                return List.Explain(list);
            }
            return $"{Atom.Explain(c.Functor)}({String.Join(", ", c.Arguments.Select(arg => Term.Explain(arg)))})";
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

        public override int GetHashCode()
        {
            return HashCode.Combine(Functor, Arity);
        }

        public static implicit operator Term(Complex rhs)
        {
            return Term.FromComplex(rhs);
        }

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
