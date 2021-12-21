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
    }
}
