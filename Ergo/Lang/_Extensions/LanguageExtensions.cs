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

    [Obsolete()]
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

    public static Maybe<SubstitutionMap> Unify(this ITerm a, ITerm b)
        => new Substitution(a, b).Unify();

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

    public static ITerm NumberVars(this ITerm term)
    {
        int i = 0;
        return Inner(term, ref i);
        ITerm Inner(ITerm term, ref int i)
        {
            if (term is Atom a)
                return a;
            if (term is Variable v)
                return new Variable($"$VAR({i++})");
            if (term is Complex c)
            {
                var args = new ITerm[c.Arguments.Length];
                for (int j = 0; j < c.Arguments.Length; j++)
                {
                    args[j] = Inner(c.Arguments[j], ref i);
                }
                term = c.WithArguments(args.ToImmutableArray());
            }
            if (term is AbstractTerm abs)
            {
                // TODO!!
                throw new InvalidOperationException();
            }
            return term;
        }
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
