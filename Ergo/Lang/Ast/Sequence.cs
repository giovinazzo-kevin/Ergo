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
        public readonly Term[] Contents;

        public Sequence(Atom functor, Term emptyElement, params Term[] args)
        {
            Root = Fold(functor, emptyElement, args);
            Contents = args;
            // Static properties
            Functor = functor;
            EmptyElement = emptyElement;
            IsEmpty = Root.Equals(emptyElement);
        }

        public static bool TryUnwrap(Term root, Term emptyElement, out Term arg, out Term next)
        {
            arg = next = default;
            if (root.Equals(emptyElement))
                return false;
            if (root.Type != TermType.Complex)
                return false;
            arg = ((Complex)root).Arguments[0];
            next = ((Complex)root).Arguments[1];
            return true;
        }

        public static IEnumerable<Term> GetContents(Term root, Term emptyElem)
        {
            while(TryUnwrap(root, emptyElem, out var arg, out root)) {
                yield return arg;
            }
        }

        public static Sequence Instantiate(Term.InstantiationContext ctx, Sequence s, bool discardsOnly = false, Dictionary<string, Variable> vars = null)
        {
            return new Sequence(s.Functor, s.EmptyElement, s.Contents.Select(t => Term.Instantiate(ctx, t, discardsOnly, vars)).ToArray());
        }

        public static Sequence Substitute(Sequence s, IEnumerable<Substitution> subs)
        {
            return new Sequence(s.Functor, s.EmptyElement, GetContents(Term.Substitute(s.Root, subs), s.EmptyElement).ToArray());
        }

    }

}
