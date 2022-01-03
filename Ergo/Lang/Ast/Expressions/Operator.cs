using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly partial struct Operator
    {
        public readonly Atom CanonicalFunctor;
        public readonly Atom[] Synonyms;
        public readonly Atom DeclaringModule;
        public readonly int Precedence;
        public readonly OperatorAffix Affix;
        public readonly OperatorAssociativity Associativity;

        public Operator(Atom module, OperatorAffix affix, OperatorAssociativity assoc, int precedence, params Atom[] functors)
        {
            DeclaringModule = module;
            Affix = affix;
            Associativity = assoc;
            Synonyms = functors;
            CanonicalFunctor = Synonyms.First();
            Precedence = precedence;
        }

        public string Explain() => $"← op({Precedence}, builtin, {CanonicalFunctor.Explain()})";
    }

}
