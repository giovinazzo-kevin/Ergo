using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Diagnostics;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Signature
    {
        public readonly Atom Functor;
        public readonly Maybe<int> Arity;

        public Signature(Atom a, Maybe<int> arity) => (Functor, Arity) = (a, arity);
        public Signature WithArity(Maybe<int> arity) => new(Functor, arity);

        public string Explain() => $"{Functor.Explain()}/{Arity.Reduce(some => some.ToString(), () => "*")}";

        public override bool Equals(object obj)
        {
            if (obj is not Signature other)
            {
                return false;
            }
            return Functor.Equals(other.Functor) && Arity.Equals(other.Arity);
        }

        public static bool operator ==(Signature left, Signature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Signature left, Signature right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Functor.GetHashCode(), Arity.GetHashCode());
        }
    }
}
