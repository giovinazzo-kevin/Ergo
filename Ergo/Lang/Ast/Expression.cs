using System;
using System.Linq;

namespace Ergo.Lang
{

    public readonly partial struct Expression
    {
        public readonly Term Left;
        public readonly Maybe<Term> Right;
        public readonly Operator Operator;
        public readonly Complex Complex;

        public Expression(Operator op, Term left, Maybe<Term> right = default)
        {
            Operator = op; 
            Left = left; 
            Right = right;
            Complex = new Complex(op.CanonicalFunctor, right.Reduce(some => new[] { left, some }, () => new[] { left }));
        }

        public static bool TryConvert(Term t, out Expression expr)
        {
            expr = default;
            if (t.Type != TermType.Complex)
                return false;
            var cplx = (Complex)t;
            if (!Operator.TryGetOperatorFromFunctor(cplx.Functor, out var op))
                return false;
            if (cplx.Arguments.Length == 1) {
                expr = op.BuildExpression(cplx.Arguments[0]);
                return true;
            }
            if (cplx.Arguments.Length == 2) {
                expr = op.BuildExpression(cplx.Arguments[0], Maybe.Some(cplx.Arguments[1]));
                return true;
            }
            return false;
        }
    }

}
