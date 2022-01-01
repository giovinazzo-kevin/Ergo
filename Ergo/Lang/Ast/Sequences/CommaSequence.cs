
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct CommaSequence : ISequence
    {
        public static readonly Atom CanonicalFunctor = new(",");
        public static readonly Atom EmptyLiteral = new("()");

        public static readonly CommaSequence Empty = new(ImmutableArray<ITerm>.Empty);

        public ITerm Root { get; }
        public Atom Functor { get; }
        public ImmutableArray<ITerm> Contents { get; }
        public ITerm EmptyElement { get; }
        public bool IsEmpty { get; }
        public bool IsParenthesized { get; }

        public CommaSequence(ImmutableArray<ITerm> args, bool parens = false)
        {
            Functor = CanonicalFunctor;
            EmptyElement = EmptyLiteral;
            Contents = args;
            IsEmpty = args.Length == 0;
            Root = ISequence.Fold(Functor, EmptyElement, args)
                .Reduce<ITerm>(a => a, v => v, c => c.Parenthesized(parens));
            IsParenthesized = parens;
        }

        public static bool TryUnfold(ITerm t, out CommaSequence expr)
        {
            expr = default;
            if (t.Equals(EmptyLiteral))
            {
                expr = new CommaSequence();
                return true;
            }
            if (t is Complex c && Operators.BinaryConjunction.Synonyms.Contains(c.Functor))
            {
                var args = ImmutableArray<ITerm>.Empty.Add(c.Arguments[0]);
                if (c.Arguments.Length == 1)
                {
                    expr = new CommaSequence(args);
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if (c.Arguments[1].Equals(EmptyLiteral))
                {
                    expr = new CommaSequence(args);
                    return true;
                }
                if (TryUnfold(c.Arguments[1], out var subExpr))
                {
                    expr = new CommaSequence(args.AddRange(subExpr.Contents));
                    return true;
                }
                else
                {
                    expr = new CommaSequence(args.Add(c.Arguments[1]));
                    return true;
                }
            }
            return false;
        }


        public string Explain(bool canonical = false)
        {
            if(IsParenthesized)
            {
                return $"({Inner(this)})";
            }
            return Inner(this);
            string Inner(CommaSequence seq)
            {
                if (seq.IsEmpty)
                {
                    return seq.EmptyElement.Explain(canonical);
                }
                var joined = string.Join(canonical ? "∧" : ",", seq.Contents.Select(t => t.Explain(canonical)));
                return joined;
            }
        }

        public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
            new CommaSequence(ImmutableArray.CreateRange(Contents.Select(arg => arg.Instantiate(ctx, vars))));

        public ISequence Substitute(IEnumerable<Substitution> subs) =>
            new CommaSequence(ImmutableArray.CreateRange(Contents.Select(arg => arg.Substitute(subs)).ToArray()));
    }
}
