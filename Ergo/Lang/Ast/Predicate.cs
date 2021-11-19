using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{

    public readonly struct Predicate
    {
        public readonly Term Head { get; }
        public readonly Sequence Body { get; }
        public readonly string Documentation { get; }

        public static string Explain(Predicate p)
        {
            if(p.Body.IsEmpty || p.Body.Contents.SequenceEqual(new Term[] { new Atom(true) })) {
                return $"{Term.Explain(p.Head)}.";
            }

            var expl = $"{Term.Explain(p.Head)} :- {CommaExpression.Explain(new CommaExpression(p.Body))}.";
            if (!String.IsNullOrWhiteSpace(p.Documentation)) {
                expl = $"{String.Join("\r\n", p.Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r))}\r\n" + expl;
            }

            return expl;
        }

        public static string Signature(Term head)
        {
            return head.Type switch
            {
                TermType.Atom => $"{Term.Explain(head)}/0"
                , TermType.Complex when (Complex)head is var c => $"{c.Functor}/{c.Arity}"
                , TermType.Variable => $"{Term.Explain(head)}/?"
                , _ => throw new InvalidOperationException(head.Type.ToString())
            };
        }

        public Predicate(string desc, Term head, Sequence body)
        {
            if(!CommaExpression.IsCommaExpression(body)) {
                throw new InvalidOperationException("Predicates may only be built out of CommaExpression sequences.");
            }
            Documentation = desc;
            Head = head;
            Body = body;
        }

        public static Predicate Instantiate(Term.InstantiationContext ctx, Predicate p, Dictionary<string, Variable> vars = null)
        {
            vars ??= new Dictionary<string, Variable>();
            return new Predicate(
                p.Documentation
                , Term.Instantiate(ctx, p.Head, vars)
                , Sequence.Instantiate(ctx, p.Body, vars)
            );
        }

        public static Predicate Substitute(Predicate k, IEnumerable<Substitution> s)
        {
            return new Predicate(k.Documentation, Term.Substitute(k.Head, s), Sequence.Substitute(k.Body, s));
        }

        public static bool TryUnify(Term head, Predicate predicate, out IEnumerable<Substitution> substitutions)
        {
            var S = new List<Substitution>();
            if (Substitution.TryUnify(new Substitution(head, predicate.Head), out var subs)) {
                S.AddRange(subs);
                substitutions = S;
                return true;
            }
            substitutions = default;
            return false;
        }

        public override string ToString()
        {
            return Explain(this);
        }
    }
}