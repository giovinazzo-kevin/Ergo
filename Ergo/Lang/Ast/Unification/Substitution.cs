
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Substitution
    {
        public readonly ITerm Lhs;
        public readonly ITerm Rhs;

        public Substitution(ITerm lhs, ITerm rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public Substitution WithRhs(ITerm newRhs)
        {
            return new Substitution(Lhs, newRhs);
        }

        public Substitution WithLhs(ITerm newLhs)
        {
            return new Substitution(newLhs, Rhs);
        }

        public static Variable[] Variables(Substitution eq) 
        {
            return eq.Lhs.Variables.Concat(eq.Rhs.Variables).ToArray();
        }

        public void Deconstruct(out ITerm lhs, out ITerm rhs)
        {
            lhs = Lhs;
            rhs = Rhs;
        }

        public bool TryUnify(out IEnumerable<Substitution> substitutions)
        {
            substitutions = default;
            // Set of equality statements
            var E = new List<Substitution>() { this };
            // Set of substitutions
            var S = new List<Substitution>();
            while (E.Count > 0) {
                var (x, y) = E[0];
                E.RemoveAt(0);
                if (!Unify(x, y))
                    return false;
            }

            substitutions = S;
            return true;

            void ApplySubstitution(Substitution s)
            {
                E = new List<Substitution>(E.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Distinct());
                S = new List<Substitution>(S.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Append(s).Distinct());
            }

            bool Unify(ITerm x, ITerm y)
            {
                if (x is Complex && Dict.TryUnfold(x, out var xd))
                    x = xd;
                if (y is Complex && Dict.TryUnfold(y, out var yd))
                    y = yd;
                if (!x.Equals(y))
                {
                    if (y is Variable)
                    {
                        ApplySubstitution(new Substitution(y, x));
                    }
                    else if (x is Variable)
                    {
                        ApplySubstitution(new Substitution(x, y));
                    }
                    else if (x is Complex cx && y is Complex cy)
                    {
                        if (!DoComplex(cx, cy))
                            return false;
                    }
                    else if (x is Dict dx && y is Dict dy)
                    {
                        if (!DoDict(dx, dy))
                            return false;
                    }
                    else if (
                        (x is Dict || y is Dict)
                        && (x is Complex { Functor: { } f } && WellKnown.Functors.Dict.Contains(f)
                            || y is Complex { Functor: { } g } && WellKnown.Functors.Dict.Contains(g)))
                    {
                        var cx1 = x is Dict d1 ? d1.CanonicalForm : (Complex)x;
                        var cy1 = y is Dict d2 ? d2.CanonicalForm : (Complex)y;
                        if (!DoComplex(cx1, cy1))
                            return false;
                    }
                    else return false;
                }
                return true;
            }

            bool DoDict(Dict dx, Dict dy)
            {
                var dxFunctor = dx.Functor.Reduce(a => (ITerm)a, v => v);
                var dyFunctor = dy.Functor.Reduce(a => (ITerm)a, v => v);
                E.Add(new Substitution(dxFunctor, dyFunctor));

                var set = dx.Dictionary.Keys.Intersect(dy.Dictionary.Keys);
                if (!set.Any() && dx.Dictionary.Count != 0 && dy.Dictionary.Count != 0)
                    return false;
                foreach (var key in set)
                {
                    E.Add(new Substitution(dx.Dictionary[key], dy.Dictionary[key]));
                }
                return true;
            }

            bool DoComplex(Complex cx, Complex cy)
            {
                if (!cx.Matches(cy))
                {
                    return false;
                }
                for (int i = 0; i < cx.Arguments.Length; i++)
                {
                    E.Add(new Substitution(cx.Arguments[i], cy.Arguments[i]));
                }
                return true;
            }
        }

        public override bool Equals(object obj)
        {
            if(obj is Substitution eq) {
                return eq.Lhs.Equals(Lhs) && eq.Rhs.Equals(Rhs);
            }
            return false;
        }

        public Substitution Inverted() => new(Rhs, Lhs);

        public override int GetHashCode()
        {
            return HashCode.Combine(Lhs, Rhs);
        }

        public string Explain()
        {
            return $"{Lhs.Explain()}/{Rhs.Explain()}";
        }
    }
}
