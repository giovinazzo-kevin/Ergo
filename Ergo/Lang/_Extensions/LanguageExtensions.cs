using Ergo.Lang.Ast.Terms.Interfaces;
using System.Reflection;

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

    public static bool IsAbstract(this ITerm t, Type type, out IAbstractTerm match)
    {
        match = default;
        if (t.AbstractForm.HasValue)
        {
            match = t.AbstractForm.GetOrThrow();
            return match.GetType().Equals(type);
        }

        // It could be that 't' is the canonical form of an abstract term but that it wasn't recognized as abstract on creation.
        // It's a bit unclear when exactly this happens, but two reasons are:
        // - When parsing the canonical form of an abstract type;
        // - When unifying an abstract term with a matching non-abstract canonical form.

        if (AbstractTermCache.IsNot(t, type))
            return false;
        if (AbstractTermCache.TryGet(t, type, out match))
            return true;

        // If the abstract type implements a static Maybe<T> FromCanonical(ITerm t) method, try calling it and caching the result.
        var resultType = typeof(Maybe<>).MakeGenericType(type);
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
            m.Name.Equals("FromCanonical") && m.GetParameters().Length == 1 && m.ReturnType.Equals(resultType)) is { } unfold)
        {
            var result = unfold.Invoke(null, new[] { t });
            if (resultType.GetField("HasValue")?.GetValue(result) is not true)
            {
                AbstractTermCache.Miss(t, type);
                return false;
            }

            match = (IAbstractTerm)resultType.GetMethod("GetOrThrow").Invoke(result, new object[] { null });
            AbstractTermCache.Set(t, match);
            return true;
        }

        return false;
    }

    public static bool IsAbstract<T>(this ITerm t, out T match)
        where T : IAbstractTerm
    {
        match = default;
        if (IsAbstract(t, typeof(T), out var untyped))
        {
            match = (T)untyped;
            return true;
        }

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
