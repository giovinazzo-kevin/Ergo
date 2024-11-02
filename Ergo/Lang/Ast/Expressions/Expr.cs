using Ergo.Modules;

namespace Ergo.Lang.Ast;

public readonly partial struct Expr
{
    public readonly ITerm Left;
    public readonly Maybe<ITerm> Right;
    public readonly Operator Operator;
    public readonly ITerm Term;

    private Expr(ITerm left, Maybe<ITerm> right, Operator op, ITerm term)
    {
        Left = left;
        Right = right;
        Operator = op;
        Term = term;
    }

    public Expr(Complex fromComplex)
    {
        var ops = WellKnown.Operators.DeclaredOperators.AsEnumerable();
        Operator = ops.Single(op => op.Synonyms.Contains(fromComplex.Functor) &&
            (op.Fixity == Fixity.Infix && fromComplex.Arguments.Length == 2
            || op.Fixity != Fixity.Infix && fromComplex.Arguments.Length == 1));
        Left = fromComplex.Arguments[0];
        Right = fromComplex.Arguments.Length > 1 ? Maybe.Some(fromComplex.Arguments[1]) : default;
        Term = fromComplex;
    }

    public Expr(Operator op, ITerm left, Maybe<ITerm> right = default, bool parenthesized = true)
    {
        Operator = op;
        Left = left;
        Right = right;
        Term = new Complex(op.CanonicalFunctor, right.Select(some => new[] { left, some }).GetOr(new[] { left }))
            .AsOperator(op)
            .AsParenthesized(parenthesized);
    }

    public Expr WithTerm(ITerm t) => new(Left, Right, Operator, t);
}

