using System.Diagnostics;

namespace Ergo.Lang.Ast;

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
        var module = Module
            .Select(some => $"{some.Explain()}{WellKnown.Functors.Module.First().Explain()}")
            .GetOr("");
        var tag = Tag
            .Select(some => some.Explain())
            .GetOr("");
        var arity = Arity
            .Select(some => some.ToString())
            .GetOr("*");
        if (WellKnown.Functors.Dict.Contains(Functor))
        {
            return $"{module}{tag}{{}}{WellKnown.Functors.Arity.First().Explain()}{arity}";
        }

        var tagSign = Tag
            .Select(_ => WellKnown.Functors.SignatureTag.First().Explain())
            .GetOr("");
        return $"{module}{Functor.Explain()}{tagSign}{tag}{WellKnown.Functors.Arity.First().Explain()}{arity}";
    }

    public override bool Equals(object obj)
    {
        if (obj is not Signature other)
        {
            return false;
        }

        return Functor.Equals(other.Functor)
            && (!Arity.TryGetValue(out var a) || !other.Arity.TryGetValue(out var b) || a.Equals(b))
            && Module.Equals(other.Module);
    }

    public static bool operator ==(Signature left, Signature right) => left.Equals(right);

    public static bool operator !=(Signature left, Signature right) => !(left == right);

    public override int GetHashCode() => HashCode.Combine(Functor.GetHashCode(), Arity.GetHashCode(), Module.GetHashCode(),
        1 - Arity.TryGetValue(out _).GetHashCode(), 1 - Module.TryGetValue(out _).GetHashCode());

    public static bool FromCanonical(ITerm term, out Signature sig)
    {
        if (term is Complex c && WellKnown.Functors.Division.Contains(c.Functor)
            && term.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
        {
            var module = c.Arguments[0].GetQualification(out var arg);
            if (arg is Complex d && WellKnown.Functors.SignatureTag.Contains(d.Functor) && d.Arguments.Length == 2)
            {
                sig = new((Atom)arg, match.Arity, module, (Atom)d.Arguments[1]);
                return true;
            }

            sig = new((Atom)arg, match.Arity, module, Maybe<Atom>.None);
            return true;
        }

        sig = default;
        return false;
    }
}
