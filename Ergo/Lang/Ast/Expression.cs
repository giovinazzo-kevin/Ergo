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
    }

}
