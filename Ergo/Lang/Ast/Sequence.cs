using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{
    /// <summary>
    /// Represents an abstract list of terms, where each term is either the empty element or a concatenation of elements.
    /// This can take the shape of an ordinary list of terms, or of a list of clauses: 
    ///     [1, 2, 3, 4] -> '[|]'(1, '[|]'(2, '[|]'(3, '[|]'(4, []))))
    ///     (fact, pred(X), true) -> ','(fact, ','(pred(X), ','(true, ())))
    /// </summary>
    public readonly partial struct Sequence
    {
        public readonly Term Root;
        public readonly Atom Functor;
        public readonly Term EmptyElement;
        public readonly bool IsEmpty;

        public Sequence(Atom functor, Term emptyElement, params Term[] args)
        {
            Root = Fold(functor, emptyElement, args);
            // Static properties
            Functor = functor;
            EmptyElement = emptyElement;
            IsEmpty = Root.Equals(emptyElement);
        }

        private Sequence(Sequence parent, Term child)
        {
            Functor = parent.Functor;
            EmptyElement = parent.EmptyElement;
            Root = child;
            IsEmpty = child.Equals(EmptyElement);
        }

        public IEnumerable<Term> GetContents() => GetContents(this);

        public static Sequence Instantiate(Term.InstantiationContext ctx, Sequence s, bool discardsOnly = false, Dictionary<string, Variable> vars = null)
        {
            return new Sequence(s.Functor, s.EmptyElement, s.GetContents().Select(t => Term.Instantiate(ctx, t, discardsOnly, vars)).ToArray());
        }

        public static string Explain(Sequence s)
        {
            if (s.IsEmpty) {
                return Term.Explain(s.EmptyElement);
            }
            var contents = s.GetContents().ToList();
            var tokens = new { Open = "{", Close = "}", Separator = ", ", WrapSingleElement = true };
            // Sugaring is applied for atomic lists and comma expressions
            if (s.Functor.Equals(List.Functor)) {
                tokens = new { Open = "[", Close = "]", Separator = ", ", WrapSingleElement = true };
            }
            else if (s.Functor.Equals(CommaExpression.Functor)) {
                tokens = new { Open = "(", Close = ")", Separator = ", ", WrapSingleElement = false };
            }

            var joined = String.Join(tokens.Separator, contents.Select(t => Term.Explain(t)));
            if (contents.Count != 1 || tokens.WrapSingleElement) {
                return $"{tokens.Open}{joined}{tokens.Close}";
            }
            return joined;
        }

        public static Sequence Substitute(Sequence s, IEnumerable<Substitution> subs)
        {
            return new Sequence(s, Term.Substitute(s.Root, subs));
        }

    }

}
