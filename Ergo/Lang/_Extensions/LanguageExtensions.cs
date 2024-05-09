using Ergo.Lang.Ast.Terms.Interfaces;
using System.Text;

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

    public static T Reduce<T>(this ITerm t, Func<Atom, T> ifAtom, Func<Variable, T> ifVariable, Func<Complex, T> ifComplex, Func<AbstractTerm, T> ifAbstract)
    {
        if (t is Atom a) return ifAtom(a);
        if (t is Variable v) return ifVariable(v);
        if (t is Complex c) return ifComplex(c);
        if (t is AbstractTerm abs) return ifAbstract(abs);
        throw new NotSupportedException(t.GetType().Name);
    }

    public static bool IsClr<T>(this ITerm t, out T match, Func<T, bool> filter = null)
    {
        if (t is Atom a && a.Value.GetType().IsAssignableTo(typeof(T)) && (filter?.Invoke((T)a.Value) ?? true))
        {
            match = (T)a.Value;
            return true;
        }

        match = default;
        return false;
    }

    public static Maybe<T> IsAbstract<T>(this ITerm t)
        where T : AbstractTerm
    {
        if (t is T abs)
            return abs;
        return default;
    }
    public static bool Matches<T>(this ITerm t, out T match, T shape = default, Func<T, bool> filter = null, Maybe<TermMarshalling> mode = default, bool matchFunctor = false)
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
        catch (Exception)
        {
            return false;
        }
    }
    public static bool MatchesUntyped(this ITerm t, out object match, Type type, Func<object, bool> filter = null, Maybe<TermMarshalling> mode = default, bool matchFunctor = false)
    {
        match = default;
        try
        {
            match = TermMarshall.FromTerm(t, type, mode);
            if (matchFunctor)
            {
                if (t is Complex cplx && !cplx.Functor.Equals(new Atom(type.Name.ToLower())))
                    return false;
            }

            return filter?.Invoke(match) ?? true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static Maybe<SubstitutionMap> Unify(this ITerm a, ITerm b, SubstitutionMap map = null)
        => new Substitution(a, b).Unify(map);

    public static Maybe<SubstitutionMap> Unify(this Predicate predicate, ITerm head)
    {
        var h = predicate.Head;
        h.GetQualification(out var qv);
        head.GetQualification(out var hv);
        return Unify(hv, qv);
    }

    public static Signature GetSignature(this ITerm term)
    {
        if (term is AbstractTerm abs)
            return abs.GetSignature();
        if (term.GetQualification(out term).TryGetValue(out var qm))
        {
            var qs = term.GetSignature();
            var tag = Maybe<Atom>.None;
            if (term is Complex cplx && WellKnown.Functors.SignatureTag.Contains(cplx.Functor))
            {
                term = cplx.Arguments[0];
                tag = (Atom)cplx.Arguments[1];
            }
            return new Signature(qs.Functor, qs.Arity, qm, tag);
        }
        return new Signature(
            term.Reduce(a => a, v => new Atom(v.Name), c => c.Functor, abs => default),
            term.Reduce(a => 0, v => 0, c => c.Arity, abs => default),
            Maybe<Atom>.None,
            term.Reduce(_ => Maybe<Atom>.None, _ => Maybe<Atom>.None, _ => Maybe<Atom>.None, _ => Maybe<Atom>.None)
        );
    }

    public static ITerm BuildAnonymousTerm(this Atom functor, int arity, bool ignoredVars = true)
    {
        if (arity == 0)
            return functor;
        return new Complex(functor, Enumerable.Range(0, arity)
            .Select(i => (ITerm)new Variable(ignoredVars ? $"__A{i}" : $"A{i}"))
            .ToArray());
    }

    public static ITerm NumberVars(this ITerm term)
    {
        return term.Instantiate(new("$VAR"), new());
    }

    public static string ToCSharpCase(this string s)
    {
        // Assume ergo_case
        var sb = new StringBuilder();
        bool nextCharIsUpper = true;
        for (var i = 0; i < s.Length; ++i)
        {
            if (s[i] == '_')
            {
                nextCharIsUpper = true; // Next character should be in upper case.
            }
            else
            {
                sb.Append(nextCharIsUpper ? char.ToUpper(s[i]) : s[i]);
                nextCharIsUpper = false;
            }
        }

        return sb.ToString();
    }

    public static string Indent(this string s, int indent, int tabWidth = 2)
    {
        return s.Split('\n').Select(s => new string(' ', indent * tabWidth) + s).Join("\n");
    }

    public static string ToErgoCase(this string s)
    {
        if (s is null)
            return null;
        // Assume PascalCase
        var wasUpper = true;
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; ++i)
        {
            var isUpper = char.IsUpper(s[i]);
            if (i > 0 && !wasUpper && isUpper && s[i - 1] != '_')
            {
                sb.Append("_");
            }

            sb.Append(char.ToLower(s[i]));
            wasUpper = isUpper;
        }

        return sb.ToString();
    }
}
