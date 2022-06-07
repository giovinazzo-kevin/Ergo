using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Solver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang.Extensions
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
        public static bool Is<T>(this ITerm t, out T match, Func<T, bool> filter = null)
        {
            if(t is Atom a && a.Value is T value && (filter?.Invoke(value) ?? true))
            {
                match = value;
                return true;
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
                if(matchFunctor)
                {
                    if (t is Complex cplx && !cplx.Functor.Equals(new Atom(typeof(T).Name.ToLower())))
                        return false;
                }
                return filter?.Invoke(match) ?? true;
            }
            catch (Exception) { return false; }
        }

        public static Signature GetSignature(this ITerm term)
        {
            if(term.TryGetQualification(out var qm, out var qv))
            {
                var qs = qv.GetSignature();
                return new Signature(
                    qs.Functor,
                    qs.Arity,
                    Maybe.Some(qm)
                );
            }
            return new Signature(
                term.Reduce(a => a, v => new Atom(v.Name), c => c.Functor),
                Maybe.Some(term.Reduce(a => 0, v => 0, c => c.Arity)),
                Maybe<Atom>.None
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
}
