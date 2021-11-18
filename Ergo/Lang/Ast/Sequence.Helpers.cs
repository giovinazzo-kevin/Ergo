
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{
    public readonly partial struct Sequence
    {
        private static Term Fold(Atom functor, Term emptyElement, params Term[] args)
        {
            if (args.Length == 0)
                return emptyElement;
            if (args.Length == 1)
                return new Complex(functor, args[0], emptyElement);
            var rev = new List<Term>(args);
            rev.Reverse();
            return rev.Prepend(emptyElement).Aggregate((a, b) => new Complex(functor, b, a));
        }

        private static IEnumerable<Term> GetContents(Sequence s)
        {
            return s.Root.Reduce(
                // a == EmptyElement
                a => Array.Empty<Term>(),
                // Unreachable
                v => throw new InvalidOperationException(),
                // Subsequence
                c => {
                    if(c.Functor.Equals(s.Functor)) {
                        if (c.Arity != 2)
                            throw new InvalidOperationException();
                        return GetContents(new Sequence(s, c.Arguments[1])).Prepend(c.Arguments[0]);
                    }
                    throw new InvalidOperationException();
                }
            );
        }
    }

}
