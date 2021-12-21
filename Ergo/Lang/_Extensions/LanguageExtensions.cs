using System;

namespace Ergo.Lang
{
    public static class LanguageExtensions
    {
        public static T Reduce<T>(this ITerm t, Func<Atom, T> ifAtom, Func<Variable, T> ifVariable, Func<Complex, T> ifComplex)
        {
            if (t is Atom a) return ifAtom(a);
            if (t is Variable v) return ifVariable(v);
            if (t is Complex c) return ifComplex(c);
            throw new NotSupportedException(t.GetType().Name);
        }

        public static bool Matches<T>(this ITerm t, out T match, T shape = default, Func<T, bool> filter = null, TermMarshall.MarshallingMode marshalling = TermMarshall.MarshallingMode.Positional)
        {
            match = default;
            try
            {
                match = TermMarshall.FromTerm(t, shape, marshalling);
                return filter?.Invoke(match) ?? true;
            }
            catch (Exception) { return false; }
        }
    }
}
