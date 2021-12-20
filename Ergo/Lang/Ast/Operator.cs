using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{
    public readonly partial struct Operator
    {
        public readonly Atom CanonicalFunctor;
        public readonly Atom[] Synonyms;
        public readonly int Precedence;
        public readonly AffixType Affix;
        public readonly AssociativityType Associativity;

        public Operator(AffixType affix, AssociativityType assoc, int precedence, params string[] functors)
        {
            Affix = affix;
            Associativity = assoc;
            Synonyms = functors.Select(s => new Atom(s)).ToArray();
            CanonicalFunctor = Synonyms.First();
            Precedence = precedence;
        }
    }

}
