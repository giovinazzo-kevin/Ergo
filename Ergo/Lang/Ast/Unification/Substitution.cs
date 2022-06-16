using System.Diagnostics;

namespace Ergo.Lang.Ast;

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

    public Substitution WithRhs(ITerm newRhs) => new(Lhs, newRhs);

    public Substitution WithLhs(ITerm newLhs) => new(newLhs, Rhs);

    public static Variable[] Variables(Substitution eq) => eq.Lhs.Variables.Concat(eq.Rhs.Variables).ToArray();

    public void Deconstruct(out ITerm lhs, out ITerm rhs)
    {
        lhs = Lhs;
        rhs = Rhs;
    }

    public Maybe<IEnumerable<Substitution>> Unify()
    {
        // Set of equality statements
        var E = new List<Substitution>() { this };
        // Set of substitutions
        var S = new List<Substitution>();
        while (E.Count > 0)
        {
            var (x, y) = E[0];
            E.RemoveAt(0);
            if (!Unify(x, y))
                return default;
        }

        return Maybe.Some(S.AsEnumerable());

        void ApplySubstitution(Substitution s)
        {
            E = new List<Substitution>(E.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Distinct());
            S = new List<Substitution>(S.Select(eq => new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s))).Append(s).Distinct());
        }

        bool Unify(ITerm x, ITerm y)
        {
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
                    if (cx.AbstractForm.HasValue && cy.AbstractForm.HasValue)
                    {
                        var u = cx.AbstractForm.GetOrThrow().Unify(cy.AbstractForm.GetOrThrow());
                        if (!u.HasValue)
                            return false;
                        E.AddRange(u.GetOrThrow());
                        return true;
                    }

                    if (!cx.Matches(cy))
                    {
                        return false;
                    }

                    for (var i = 0; i < cx.Arguments.Length; i++)
                    {
                        E.Add(new Substitution(cx.Arguments[i], cy.Arguments[i]));
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is Substitution eq)
        {
            return eq.Lhs.Equals(Lhs) && eq.Rhs.Equals(Rhs);
        }

        return false;
    }

    public Substitution Inverted() => new(Rhs, Lhs);

    public override int GetHashCode() => HashCode.Combine(Lhs, Rhs);

    public string Explain() => $"{Lhs.Explain()}/{Rhs.Explain()}";
}
