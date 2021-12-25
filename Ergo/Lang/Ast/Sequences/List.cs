﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct List : ISequence
    {
        public static readonly Atom CanonicalFunctor = new("[|]");
        public static readonly Atom EmptyLiteral = new("[]");

        public static readonly List Empty = new(Array.Empty<ITerm>());

        public ITerm Root { get; }
        public Atom Functor { get; }
        public ITerm[] Contents { get; }
        public ITerm EmptyElement { get; }
        public bool IsEmpty { get; }

        public readonly ITerm Tail;

        public List(ITerm[] head, Maybe<ITerm> tail = default)
        {
            Functor = CanonicalFunctor;
            EmptyElement = EmptyLiteral;
            Contents = head;
            IsEmpty = head.Length == 0;
            Tail = tail.Reduce(some => some, () => EmptyLiteral);
            //if(!Tail.Equals(EmptyElement))
            //{
            //    Contents = Contents.Append(tail);
            //    IsEmpty &= tail.Equals(EmptyElement);
            //}
            Root = ISequence.Fold(Functor, Tail, head);
        }

        public static bool TryUnfold(ITerm t, out List expr)
        {
            expr = default;
            if (t.Equals(EmptyLiteral)) {
                expr = Empty;
                return true;
            }
            if (t is Complex c && CanonicalFunctor.Equals(c.Functor)) {
                var args = new List<ITerm>() { c.Arguments[0] };
                if (c.Arguments.Length == 1 || c.Arguments[1].Equals(EmptyLiteral)) {
                    expr = new List(args.ToArray());
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if (TryUnfold(c.Arguments[1], out var subExpr)) {
                    args.AddRange(subExpr.Contents);
                    expr = new List(args.ToArray(), Maybe.Some(subExpr.Tail));
                    return true;
                }
                else {
                    expr = new List(args.ToArray(), Maybe.Some(c.Arguments[1]));
                    return true;
                }
            }
            return false;
        }

        public string Explain()
        {
            if (IsEmpty) {
                return Tail.Explain();
            }
            var joined = String.Join(", ", Contents.Select(t => t.Explain()));
            if(!Tail.Equals(EmptyElement)) {
                return $"[{joined}|{Tail.Explain()}]";
            }
            return $"[{joined}]";
        }

        public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
            new List(Contents.Select(arg => arg.Instantiate(ctx, vars)).ToArray(), Maybe.Some(Tail.Instantiate(ctx, vars)));

        public ISequence Substitute(IEnumerable<Substitution> subs) =>
            new List(Contents.Select(arg => arg.Substitute(subs)).ToArray(), Maybe.Some(Tail.Substitute(subs)));
    }

}