using Ergo.Interpreter;

namespace Ergo.Lang.Ast;

public readonly partial struct Expression
{
    public readonly ITerm Left;
    public readonly Maybe<ITerm> Right;
    public readonly Operator Operator;
    public readonly ITerm Term;

    private Expression(ITerm left, Maybe<ITerm> right, Operator op, ITerm term)
    {
        Left = left;
        Right = right;
        Operator = op;
        Term = term;
    }

    public Expression(Complex fromComplex, Maybe<InterpreterScope> maybeScope = default)
    {
        var ops = WellKnown.Operators.DeclaredOperators.AsEnumerable();
        if (maybeScope.TryGetValue(out var scope))
        {
            ops = ops.Concat(scope.VisibleOperators)
                .Distinct();
        }
        Operator = ops.Single(op => op.Synonyms.Contains(fromComplex.Functor) &&
            (op.Fixity == Fixity.Infix && fromComplex.Arity == 2
            || op.Fixity != Fixity.Infix && fromComplex.Arity == 1));
        Left = fromComplex.Arguments[0];
        Right = fromComplex.Arity > 1 ? Maybe.Some(fromComplex.Arguments[1]) : default;
        Term = fromComplex;
    }

    public Expression(Operator op, ITerm left, Maybe<ITerm> right = default, bool parenthesized = true)
    {
        Operator = op;
        Left = left;
        Right = right;
        Term = new Complex(op.CanonicalFunctor, right.Select(some => new[] { left, some }).GetOr(new[] { left }))
            .AsOperator(op)
            .AsParenthesized(parenthesized);
    }

    public Expression WithTerm(ITerm t) => new(Left, Right, Operator, t);
}

