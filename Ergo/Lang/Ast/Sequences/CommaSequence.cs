
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct CommaSequence : ISequence
    {
        public static readonly Atom CanonicalFunctor = new(",");
        public static readonly Atom EmptyLiteral = new("()");

        public ITerm Root { get; }
        public Atom Functor { get; }
        public ITerm[] Contents { get; }
        public ITerm EmptyElement { get; }
        public bool IsEmpty { get; }

        public CommaSequence(params ITerm[] args) 
        {
            Functor = CanonicalFunctor;
            EmptyElement = EmptyLiteral;
            Contents = args;
            IsEmpty = args.Length == 0;
            Root = ISequence.Fold(Functor, EmptyElement, args);
        }

        public static bool TryUnfold(ITerm t, out CommaSequence expr)
        {
            expr = default;
            if(t.Equals(EmptyLiteral)) {
                expr = new CommaSequence();
                return true;
            }
            if(t is Complex c && Operators.BinaryConjunction.Synonyms.Contains(c.Functor)) {
                var args = new List<ITerm>() { c.Arguments[0] };
                if (c.Arguments.Length == 1) {
                    expr = new CommaSequence(args.ToArray());
                    return true;
                }
                if (c.Arguments.Length != 2)
                    return false;
                if(c.Arguments[1].Equals(EmptyLiteral)) {
                    expr = new CommaSequence(args.ToArray());
                    return true;
                }
                if (TryUnfold(c.Arguments[1], out var subExpr)) {
                    args.AddRange(subExpr.Contents);
                    expr = new CommaSequence(args.ToArray());
                    return true;
                }
                else {
                    args.Add(c.Arguments[1]);
                    expr = new CommaSequence(args.ToArray());
                    return true;
                }
            }
            return false;
        }


        public string Explain()
        {
            if (IsEmpty) {
                return EmptyElement.Explain();
            }
            var joined = string.Join(", ", Contents.Select(t => t.Explain()));
            if (Contents.Length != 1) {
                return $"({joined})";
            }
            return joined;
        }

        public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
            new CommaSequence(Contents.Select(arg => arg.Instantiate(ctx, vars)).ToArray());

        public ISequence Substitute(IEnumerable<Substitution> subs) =>
            new CommaSequence(Contents.Select(arg => arg.Substitute(subs)).ToArray());
    }
}
