﻿namespace Ergo.Lang.Ast;

public readonly partial struct Expression
{
    public readonly ITerm Left;
    public readonly Maybe<ITerm> Right;
    public readonly Operator Operator;
    public readonly Complex Complex;

    public Expression(Operator op, ITerm left, Maybe<ITerm> right = default, bool parenthesized = true)
    {
        Operator = op;
        Left = left;
        Right = right;
        Complex = new Complex(op.CanonicalFunctor, right.Select(some => new[] { left, some }).GetOr(new[] { left }))
            .AsOperator(op.Affix)
            .AsParenthesized(parenthesized);
    }
}

