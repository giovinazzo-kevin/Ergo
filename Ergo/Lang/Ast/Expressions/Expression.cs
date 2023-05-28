using Ergo.Interpreter;

namespace Ergo.Lang.Ast;

public readonly partial struct Expression
{
    public readonly ITerm Left;
    public readonly Maybe<ITerm> Right;
    public readonly Operator Operator;
    public readonly Complex Complex;

    public Expression(Complex fromComplex, Maybe<InterpreterScope> maybeScope = default)
    {
        var ops = WellKnown.Operators.DefinedOperators.AsEnumerable();
        if (maybeScope.TryGetValue(out var scope))
        {
            ops = ops.Concat(scope.GetOperators())
                .Distinct();
        }
        Operator = ops.Single(op => op.Synonyms.Contains(fromComplex.Functor) &&
            (op.Fixity == Fixity.Infix && fromComplex.Arguments.Length == 2
            || op.Fixity != Fixity.Infix && fromComplex.Arguments.Length == 1));
        Left = fromComplex.Arguments[0];
        Right = fromComplex.Arguments.Length > 1 ? Maybe.Some(fromComplex.Arguments[1]) : default;
        Complex = fromComplex;
    }

    public Expression(Operator op, ITerm left, Maybe<ITerm> right = default, bool parenthesized = true)
    {
        Operator = op;
        Left = left;
        Right = right;
        Complex = new Complex(op.CanonicalFunctor, right.Select(some => new[] { left, some }).GetOr(new[] { left }))
            .AsOperator(op.Fixity)
            .AsParenthesized(parenthesized);
    }
}

