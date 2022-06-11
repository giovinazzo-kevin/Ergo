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
        public readonly Atom Functor;
        public readonly Maybe<Atom> Module;
        public readonly Maybe<Atom> Tag;
        public readonly Maybe<int> Arity;

        public Signature(Atom a, Maybe<int> arity, Maybe<Atom> module, Maybe<Atom> tag) => (Functor, Arity, Module, Tag) = (a, arity, module, tag);
        public Signature WithFunctor(Atom functor) => new(functor, Arity, Module, Tag);
        public Signature WithArity(Maybe<int> arity) => new(Functor, arity, Module, Tag);
        public Signature WithModule(Maybe<Atom> module) => new(Functor, Arity, module, Tag);
        public Signature WithTag(Maybe<Atom> tag) => new(Functor, Arity, Module, tag);

        public string Explain()
        {
            var module = Module.Reduce(some => $"{some.Explain()}{WellKnown.Functors.Module.First().Explain()}", () => "");
            var tag = Tag.Reduce(some => some.Explain(), () => null);
            var arity = Arity.Reduce(some => some.ToString(), () => "*");
            return $"{module}{Functor.Explain()}{(tag != null ? WellKnown.Functors.Subtraction.First().Explain() : null)}{tag}/{arity}";
        }

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
                if(qs is Complex d && WellKnown.Functors.Subtraction.Contains(d.Functor) && d.Arguments.Length == 2)
                {
                    sig = new((Atom)d.Arguments[0], Maybe.Some(match.Arity), Maybe.Some(qm), Maybe.Some((Atom)d.Arguments[1]));
                    return true;
                }
                sig = new((Atom)qs, Maybe.Some(match.Arity), Maybe.Some(qm), Maybe<Atom>.None);
                return true;
            }
            sig = default;
            return false;
        }
    }
}
