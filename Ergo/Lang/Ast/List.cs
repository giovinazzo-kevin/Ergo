
using System;
using System.Linq;

namespace Ergo.Lang
{
    public readonly struct List
    {
        public readonly Sequence Sequence;
        public List(Sequence from) { Sequence = from; }

        public readonly static Atom Functor = new Atom("[|]");
        public readonly static Term EmptyLiteral = new Atom("[]");
        public static Sequence Build(params Term[] args) => new Sequence(Functor, EmptyLiteral, args);
        public static bool IsList(Sequence s) => s.Functor.Equals(Functor);
        public static bool IsList(Complex c) => c.Functor.Equals(Functor);
        public static bool TryUnfold(Term t, out List expr)
        {
            expr = default;
            if (t.Equals(EmptyLiteral)) {
                expr = new List(new Sequence(Functor, EmptyLiteral));
                return true;
            }
            if (t.Type == TermType.Complex && (Complex)t is var c && Functor.Equals(c.Functor)) {
                var args = new System.Collections.Generic.List<Term>() { c.Arguments[0] };
                if (c.Arguments.Length == 1) {
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if (c.Arguments[1].Equals(EmptyLiteral)) {
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                if (TryUnfold(c.Arguments[1], out var subExpr)) {
                    args.AddRange(subExpr.Sequence.GetContents());
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
                else {
                    args.Add(c.Arguments[1]);
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()));
                    return true;
                }
            }
            return false;
        }
    }

}
