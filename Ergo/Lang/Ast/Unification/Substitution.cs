
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
                if (!x.Equals(y)) {
                    if(y is Variable) {
                        ApplySubstitution(new Substitution(y, x));
                    }
                    else if (x is Variable) {
                        ApplySubstitution(new Substitution(x, y));
                    }
                    else if (x is Complex && y is Complex) {
                        var cx = (Complex)x;
                        var cy = (Complex)y;

                        if (cx.Matches(cy)) {
                            for (int i = 0; i < cx.Arguments.Length; i++) {
                                E.Add(new Substitution(cx.Arguments[i], cy.Arguments[i]));
                            }
                        }
                        else return false;
                    }
                    else return false;
                }
            }

            substitutions = S;
            return true;

            void ApplySubstitution(Substitution s)
            {
                E = new List<Substitution>(E.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Distinct());
                S = new List<Substitution>(S.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Append(s).Distinct());
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
