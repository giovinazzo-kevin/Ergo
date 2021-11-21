
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct List
    {
        public readonly Sequence Head;
        public readonly Term Tail;
        public readonly Term Root;
        public List(Sequence head, Term tail) 
        { 
            Head = head; 
            Tail = tail;
            Root = new Sequence(Functor, tail, head.Contents).Root;
        }

        public readonly static Atom Functor = new("[|]");
        public readonly static Term EmptyLiteral = new Atom("[]");
        public static Sequence Build(params Term[] args) => new(Functor, EmptyLiteral, args);
        public static bool IsList(Sequence s) => s.Functor.Equals(Functor);
        public static bool IsList(Complex c) => c.Functor.Equals(Functor);
        public static bool TryUnfold(Term t, out List expr)
        {
            expr = default;
            if (t.Equals(EmptyLiteral)) {
                expr = new List(new Sequence(Functor, EmptyLiteral), EmptyLiteral);
                return true;
            }
            if (t.Type == TermType.Complex && (Complex)t is var c && Functor.Equals(c.Functor)) {
                var args = new List<Term>() { c.Arguments[0] };
                if (c.Arguments.Length == 1 || c.Arguments[1].Equals(EmptyLiteral)) {
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()), EmptyLiteral);
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if (TryUnfold(c.Arguments[1], out var subExpr)) {
                    args.AddRange(subExpr.Head.Contents);
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()), subExpr.Tail);
                    return true;
                }
                else {
                    expr = new List(new Sequence(Functor, EmptyLiteral, args.ToArray()), c.Arguments[1]);
                    return true;
                }
            }
            return false;
        }

        public static string Explain(List list)
        {
            if (list.Head.IsEmpty) {
                return Term.Explain(list.Tail);
            }
            var joined = String.Join(", ", list.Head.Contents.Select(t => Term.Explain(t)));
            if(!list.Tail.Equals(list.Head.EmptyElement)) {
                return $"[{joined}|{Term.Explain(list.Tail)}]";
            }
            return $"[{joined}]";
        }
    }

}
