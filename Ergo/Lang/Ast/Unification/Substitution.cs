using Ergo.Lang.Ast.Terms.Interfaces;
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

    public static Maybe<SubstitutionMap> Unify(Substitution s) => s.Unify();

    public Maybe<SubstitutionMap> Unify()
    {
        var map = new SubstitutionMap();
        var E = new Queue<Substitution>();
        var S = new Queue<Substitution>();
        E.Enqueue(this);
        while (E.TryDequeue(out var sub))
        {
            if (!Unify(sub.Lhs, sub.Rhs))
                return default;
        }
        map.AddRange(S);
        return Maybe.Some(map);
        bool Unify(ITerm x, ITerm y)
        {
            if (x.Equals(y))
                return true;
            if (x.IsGround && y.IsGround)
                return false;
            if (y is AbstractTerm ay && ay.Unify(x).TryGetValue(out var ayWithX))
            {
                foreach (var s in ayWithX)
                {
                    if (x is Variable)
                        ApplySubstitution(new Substitution(x, y));
                    else E.Enqueue(s);
                }
                return true;
            }
            else if (x is AbstractTerm ax && ax.Unify(y).TryGetValue(out var axWithY))
            {
                foreach (var s in axWithY)
                {
                    if (y is Variable)
                        ApplySubstitution(new Substitution(y, x));
                    else E.Enqueue(s);
                }
                return true;
            }
            else if (y is Variable)
            {
                ApplySubstitution(new Substitution(y, x));
                return true;
            }
            else if (x is Variable)
            {
                ApplySubstitution(new Substitution(x, y));
                return true;
            }
            else if (x is Complex cx && y is Complex cy)
            {
                if (!cx.Matches(cy))
                    return false;
                for (var i = 0; i < cx.Arguments.Length; i++)
                    E.Enqueue(new Substitution(cx.Arguments[i], cy.Arguments[i]));
                return true;
            }
            return false;
        }

        void ApplySubstitution(Substitution s)
        {
            for (int i = 0; i < E.Count; i++)
            {
                var eq = E.Dequeue();
                E.Enqueue(new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s)));
            }
            for (int i = 0; i < S.Count; i++)
            {
                var eq = S.Dequeue();
                S.Enqueue(new Substitution(eq.Lhs.Substitute(s), eq.Rhs.Substitute(s)));
            }
            S.Enqueue(s);
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

    public static bool operator ==(Substitution left, Substitution right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Substitution left, Substitution right)
    {
        return !(left == right);
    }
}
