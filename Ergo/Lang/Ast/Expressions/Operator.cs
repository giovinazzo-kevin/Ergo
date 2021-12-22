using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang.Ast
{
    public readonly partial struct Operator
    {
        public readonly Atom CanonicalFunctor;
        public readonly Atom[] Synonyms;
        public readonly int Precedence;
        public readonly OperatorAffix Affix;
        public readonly OperatorAssociativity Associativity;

        public Operator(OperatorAffix affix, OperatorAssociativity assoc, int precedence, params string[] functors)
        {
            Affix = affix;
            Associativity = assoc;
            Synonyms = functors.Select(s => new Atom(s)).ToArray();
            CanonicalFunctor = Synonyms.First();
            Precedence = precedence;
        }
    }

}
