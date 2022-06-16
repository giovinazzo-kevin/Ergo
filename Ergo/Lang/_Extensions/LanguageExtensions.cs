using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Extensions;

public static class LanguageExtensions
{
    public static T Reduce<T>(this ITerm t, Func<Atom, T> ifAtom, Func<Variable, T> ifVariable, Func<Complex, T> ifComplex)
    {
        if (t is Atom a) return ifAtom(a);
        if (t is Variable v) return ifVariable(v);
        if (t is Complex c) return ifComplex(c);
        throw new NotSupportedException(t.GetType().Name);
    }

    public static T Map<T>(this ITerm t, Func<Atom, T> ifAtom, Func<Variable, T> ifVariable, Func<Complex, T> ifComplex)
    {
        if (t is Atom a) return ifAtom(a);
        if (t is Variable v) return ifVariable(v);
        if (t is Complex c) return ifComplex(c);
        throw new NotSupportedException(t.GetType().Name);
    }

    public static bool IsClr<T>(this ITerm t, out T match, Func<T, bool> filter = null)
    {
        if (t is Atom a && a.Value is T value && (filter?.Invoke(value) ?? true))
        {
            match = value;
            return true;
        }

        match = default;
        return false;
    }

    public static bool IsAbstractTerm<T>(this ITerm t, out T match)
        where T : IAbstractTerm
    {
        if (t is Complex c && c.AbstractForm.HasValue)
        {
            var abs = c.AbstractForm.GetOrThrow();
            if (abs is T tAbs)
            {
                match = tAbs;
                return true;
            }
        }

        match = default;
        return false;
    }

    public static bool Matches<T>(this ITerm t, out T match, T shape = default, Func<T, bool> filter = null, TermMarshalling mode = TermMarshalling.Positional, bool matchFunctor = false)
    {
        match = default;
        try
        {
            match = TermMarshall.FromTerm(t, shape, Maybe.Some(mode));
            if (matchFunctor)
            {
                if (t is Complex cplx && !cplx.Functor.Equals(new Atom(typeof(T).Name.ToLower())))
                    return false;
            }

            return filter?.Invoke(match) ?? true;
        }
        catch (Exception) { return false; }
    }

    public static Maybe<IEnumerable<Substitution>> Unify(this ITerm a, ITerm b) => new Substitution(a, b).Unify();

    public static Maybe<IEnumerable<Substitution>> Unify(this Predicate predicate, ITerm head)
    {
        predicate.Head.TryGetQualification(out _, out var qv);
        head.TryGetQualification(out _, out var hv);
        return hv.Unify(qv);
    }

    public static Signature GetSignature(this ITerm term)
    {
        if (term.TryGetQualification(out var qm, out var qv))
        {
            var qs = qv.GetSignature();
            var tag = Maybe<Atom>.None;
            if (qv is Complex cplx && WellKnown.Functors.SignatureTag.Contains(cplx.Functor))
            {
                qv = cplx.Arguments[0];
                tag = Maybe.Some((Atom)cplx.Arguments[1]);
            }

            if (qv is { AbstractForm: { HasValue: true } abs })
            {
                var sig = abs.GetOrThrow().Signature
                    .WithModule(Maybe.Some(qm));
                if (tag.HasValue)
                    return sig.WithTag(tag);
                return sig;
            }

            return new Signature(
                qs.Functor,
                qs.Arity,
                Maybe.Some(qm),
                tag
            );
        }

        return new Signature(
            term.Reduce(a => a, v => new Atom(v.Name), c => c.Functor),
            term.Map(a => Maybe.Some(0), v => Maybe.Some(0), c => Maybe.Some(c.Arity)),
            Maybe<Atom>.None,
            term.Reduce(_ => Maybe<Atom>.None, _ => Maybe<Atom>.None, _ => Maybe<Atom>.None)
        );
    }

    public static ITerm BuildAnonymousTerm(this Atom functor, int arity)
    {
        if (arity == 0)
            return functor;
        return new Complex(functor, Enumerable.Range(0, arity)
            .Select(i => (ITerm)new Variable($"__A{i}"))
            .ToArray());
    }
}
