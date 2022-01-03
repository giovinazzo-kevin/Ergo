using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Signature
    {
        public readonly Maybe<Atom> Module;
        public readonly Atom Functor;
        public readonly Maybe<int> Arity;

        public Signature(Atom a, Maybe<int> arity, Maybe<Atom> module) => (Functor, Arity, Module) = (a, arity, module);
        public Signature WithArity(Maybe<int> arity) => new(Functor, arity, Module);
        public Signature WithModule(Maybe<Atom> module) => new(Functor, Arity, module);

        public string Explain() => $"{Module.Reduce(some => $"{some.Explain()}:", () => "")}{Functor.Explain()}/{Arity.Reduce(some => some.ToString(), () => "*")}";

        public override bool Equals(object obj)
        {
            if (obj is not Signature other)
            {
                return false;
            }
            return Functor.Equals(other.Functor) && Arity.Equals(other.Arity) && Module.Equals(other.Module);
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
            return HashCode.Combine(Functor.GetHashCode(), Arity.GetHashCode(), Module.GetHashCode());
        }


        public static bool TryUnfold(ITerm term, out Signature sig)
        {
            if (term is Complex c && WellKnown.Functors.Division.Contains(c.Functor)
                && term.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
            {
                c.Arguments[0].TryGetQualification(out var qm, out var qs);
                sig = new((Atom)qs, Maybe.Some(match.Arity), Maybe.Some(qm));
                return true;
            }
            sig = default;
            return false;
        }
    }
}
