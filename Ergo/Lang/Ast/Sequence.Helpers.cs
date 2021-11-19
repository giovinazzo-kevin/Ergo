
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
    }

}
