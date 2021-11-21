
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{

    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct CommaExpression
    {
        public readonly Sequence Sequence;
        public CommaExpression(Sequence from) { Sequence = from; }

        public readonly static Atom Functor = new Atom(",");
        public readonly static Term EmptyLiteral = new Atom("()");
        public static Sequence Build(params Term[] args) => new Sequence(Functor, EmptyLiteral, args);
        public static bool IsCommaExpression(Sequence s) => s.Functor.Equals(Functor);
        public static bool IsExpression(Complex c) => c.Functor.Equals(Functor);
        public static bool TryUnfold(Term t, out CommaExpression expr)
        {
            expr = default;
            if(t.Equals(EmptyLiteral)) {
                expr = new CommaExpression(new Sequence(Functor, EmptyLiteral));
                return true;
            }
            if(t.Type == TermType.Complex && (Complex)t is var c && Operators.BinaryConjunction.Synonyms.Contains(c.Functor)) {
                var args = new List<Term>() { c.Arguments[0] };
                if (c.Arguments.Length == 1) {
                    expr = new CommaExpression(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if(c.Arguments[1].Equals(EmptyLiteral)) {
                    expr = new CommaExpression(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                if (TryUnfold(c.Arguments[1], out var subExpr)) {
                    args.AddRange(subExpr.Sequence.Contents);
                    expr = new CommaExpression(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                else {
                    args.Add(c.Arguments[1]);
                    expr = new CommaExpression(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
            }
            return false;
        }


        public static string Explain(CommaExpression expr)
        {
            if (expr.Sequence.IsEmpty) {
                return Term.Explain(expr.Sequence.EmptyElement);
            }
            var joined = String.Join(", ", expr.Sequence.Contents.Select(t => Term.Explain(t)));
            if (expr.Sequence.Contents.Length != 1) {
                return $"({joined})";
            }
            return joined;
        }
    }
}
