
using System;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly partial struct Complex : IComparable<Term>
    {
        public readonly Atom Functor;
        public readonly Term[] Arguments;
        public readonly int Arity => Arguments.Length;

        private readonly int HashCode;

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

        public static Complex OfArity(Atom functor, int arity) => 
            new(functor, Enumerable.Range(0, arity).Select(i => (Term)new Variable($"_{i}")).ToArray());

        public Complex(Atom functor, params Term[] args)
        {
            Functor = functor;
            Arguments = args;
            HashCode = System.HashCode.Combine(Functor, Arguments.Length);
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
            return HashCode;
        }

        public int CompareTo(Term other)
        {
            return other.Type switch {
                TermType.Atom => this.CompareTo((Atom)other)
                , TermType.Variable => this.CompareTo((Variable)other)
                , TermType.Complex => this.CompareTo((Complex)other)
                , _ => throw new InvalidOperationException(other.Type.ToString())
            };
        }
        public int CompareTo(Atom _) => 1;
        public int CompareTo(Variable _) => 1;
        public int CompareTo(Complex other)
        {
            if (Arity.CompareTo(other.Arity) is var cmpArity && cmpArity != 0)
                return cmpArity;
            if (Functor.CompareTo(other.Functor) is var cmpFunctor && cmpFunctor != 0)
                return cmpFunctor;
            return Arguments.Select((a, i) => a.CompareTo(other.Arguments[i]))
                .DefaultIfEmpty(0)
                .FirstOrDefault(cmp => cmp != 0);
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
