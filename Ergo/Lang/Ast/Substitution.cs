
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Substitution
    {
        public readonly Term Lhs;
        public readonly Term Rhs;

        public Substitution(Term lhs, Term rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public Substitution WithRhs(Term newRhs)
        {
            return new Substitution(Lhs, newRhs);
        }

        public Substitution WithLhs(Term newLhs)
        {
            return new Substitution(newLhs, Rhs);
        }

        public static Variable[] Variables(Substitution eq) 
        {
            return Term.Variables(eq.Lhs).Concat(Term.Variables(eq.Rhs)).ToArray();
        }

        public void Deconstruct(out Term lhs, out Term rhs)
        {
            lhs = Lhs;
            rhs = Rhs;
        }

        public static bool TryUnify(Substitution eq, out IEnumerable<Substitution> substitutions)
        {
            substitutions = default;
            // Set of equality statements
            var E = new List<Substitution>() { eq };
            // Set of substitutions
            var S = new List<Substitution>();
            while (E.Count > 0) {
                var (x, y) = E[0];
                E.RemoveAt(0);
                if (!x.Equals(y)) {
                    if(y.Type == TermType.Variable) {
                        ApplySubstitution(new Substitution(y, x));
                    }
                    else if (x.Type == TermType.Variable) {
                        ApplySubstitution(new Substitution(x, y));
                    }
                    else if (x.Type == TermType.Complex && y.Type == TermType.Complex) {
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
                E = new List<Substitution>(E.Select(eq => new Substitution(Term.Substitute(eq.Lhs, s), Term.Substitute(eq.Rhs, s))).Distinct());
                S = new List<Substitution>(S.Select(eq => new Substitution(Term.Substitute(eq.Lhs, s), Term.Substitute(eq.Rhs, s))).Append(s).Distinct());
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

        public static string Explain(Substitution s)
        {
            return $"{Term.Explain(s.Lhs)}/{Term.Explain(s.Rhs)}";
        }
    }
}
