using System;
using System.Linq;

namespace Ergo.Lang
{
    public readonly partial struct Operator
    {
        public readonly Atom CanonicalFunctor;
        public readonly Atom[] Synonyms;
        public readonly int Precedence;
        public readonly AffixType Affix;
        public readonly AssociativityType Associativity;

        public Operator(AffixType affix, AssociativityType assoc, int precedence, params string[] functors)
        {
            Affix = affix;
            Associativity = assoc;
            Synonyms = functors.Select(s => new Atom(s)).ToArray();
            CanonicalFunctor = Synonyms.First();
            Precedence = precedence;
        }

        public static bool TryGetOperatorFromFunctor(Atom functor, out Operator op)
        {
            op = default;
            var match = Operators.DefinedOperators.Where(op => op.Synonyms.Any(s => functor.Equals(s)));
            if (!match.Any()) {
                return false;
            }
            op = match.Single();
            return true;
        }

        public Expression BuildExpression(Term lhs, Maybe<Term> maybeRhs = default)
        {
            var _this = this;
            return maybeRhs.Reduce(
                rhs => Associate(lhs, rhs), 
                ()  => new Expression(_this, lhs, Maybe<Term>.None)
            );

            Expression Associate(Term lhs, Term rhs)
            {
                // When the lhs represents an expression with the same precedence as this (and thus associativity, by design)
                // and right associativity, we have to swap the arguments around until they look right.
                if(Expression.TryConvert(lhs, out var lhsExpr)
                && lhsExpr.Operator.Affix == AffixType.Infix
                && lhsExpr.Operator.Associativity == AssociativityType.Right
                && lhsExpr.Operator.Precedence == _this.Precedence) {
                    // a, b, c -> ','(','(','(a, b), c)) -> ','(a, ','(b, ','(c))
                    var lhsRhs = lhsExpr.Right.Reduce(x => x, () => throw new InvalidOperationException());
                    var newRhs = lhsExpr.Operator.BuildExpression(lhsRhs, Maybe.Some(rhs));
                    return _this.BuildExpression(lhsExpr.Left, Maybe.Some<Term>(newRhs.Complex));
                }
                return new Expression(_this, lhs, Maybe.Some(rhs));
            }
        }
    }

}
