using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Utils;
using System.Reflection;

namespace Ergo.Lang.Extensions;
public static class LanguageExtensions
{
    public static string Join<T>(this IEnumerable<T> source, Func<T, string> toString, string separator = ",")
    {
        toString ??= t => t?.ToString() ?? string.Empty;
        return string.Join(separator, source.Select(toString));
    }
    public static string Join<T>(this IEnumerable<T> source, string separator = ",") => Join(source, null, separator);

    public static bool IsNumericType(this object o)
    {
        var typecode = Type.GetTypeCode(o is Type t ? t : o.GetType());
        return typecode switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
            _ => false,
        };
    }

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

    public static Maybe<IAbstractTerm> IsAbstract(this ITerm t, Type type)
    {
        // It could be that 't' is the canonical form of an abstract term but that it wasn't recognized as abstract on creation.
        // It's a bit unclear when exactly this happens, but two reasons are:
        // - When parsing the canonical form of an abstract type;
        // - When unifying an abstract term with a matching non-abstract canonical form.
        return t.AbstractForm
            .Where(a => a.GetType().Equals(type))
            .Or(() => Maybe.Some(t)
                .Where(t => !AbstractTermCache.IsNot(t, type))
                .Map(t => AbstractTermCache.Get(t, type)))
            .Or(() => Inner());

        Maybe<IAbstractTerm> Inner()
        {
            // If the abstract type implements a static Maybe<T> FromCanonical(ITerm t) method, try calling it and caching the result.
            var resultType = typeof(Maybe<>).MakeGenericType(type);
            if (type.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
                m.Name.Equals("FromCanonical") && m.GetParameters().Length == 1 && m.ReturnType.Equals(resultType)) is { } unfold)
            {
                var result = unfold.Invoke(null, new[] { t });
                if (resultType.GetField("HasValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(result) is not true)
                {
                    AbstractTermCache.Miss(t, type);
                    return default;
                }

                var match = (IAbstractTerm)resultType.GetMethod("GetOrThrow").Invoke(result, new object[] { null });
                AbstractTermCache.Set(t, match);
                return Maybe.Some(match);
            }

            return default;
        }
    }

    public static Maybe<T> IsAbstract<T>(this ITerm t)
        where T : IAbstractTerm
    {
        return IsAbstract(t, typeof(T))
            .Select(a => (T)a);
    }

    public static bool Matches<T>(this ITerm t, out T match, T shape = default, Func<T, bool> filter = null, TermMarshalling mode = TermMarshalling.Positional, bool matchFunctor = false)
    {
        match = default;
        try
        {
            match = TermMarshall.FromTerm(t, shape, mode);
            if (matchFunctor)
            {
                if (t is Complex cplx && !cplx.Functor.Equals(new Atom(typeof(T).Name.ToLower())))
                    return false;
            }

            return filter?.Invoke(match) ?? true;
        }
        catch (Exception) { return false; }
    }

    public static Maybe<SubstitutionMap> Unify(this ITerm a, ITerm b) => new Substitution(a, b).Unify();

    public static Maybe<SubstitutionMap> Unify(this Predicate predicate, ITerm head)
    {
        predicate.Head.GetQualification(out var qv);
        head.GetQualification(out var hv);
        return qv.Unify(hv);
    }

    public static Signature GetSignature(this ITerm term)
    {
        if (term.GetQualification(out term).TryGetValue(out var qm))
        {
            var qs = term.GetSignature();
            var tag = Maybe<Atom>.None;
            if (term is Complex cplx && WellKnown.Functors.SignatureTag.Contains(cplx.Functor))
            {
                term = cplx.Arguments[0];
                tag = (Atom)cplx.Arguments[1];
            }

            if (term.AbstractForm.TryGetValue(out var abs))
            {
                var sig = abs.Signature
                    .WithModule(qm);
                if (tag.TryGetValue(out _))
                    return sig.WithTag(tag);
                return sig;
            }

            return new Signature(qs.Functor, qs.Arity, qm, tag);
        }

        return new Signature(
            term.Reduce(a => a, v => new Atom(v.Name), c => c.Functor),
            term.Map(a => 0, v => 0, c => c.Arity),
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
