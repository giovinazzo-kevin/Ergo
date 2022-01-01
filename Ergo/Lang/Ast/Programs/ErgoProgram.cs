using Ergo.Interpreter.Directives;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{


    [DebuggerDisplay("{ Explain() }")]
    public readonly struct ErgoProgram : IExplainable
    {
        public readonly Directive[] Directives;
        public readonly KnowledgeBase KnowledgeBase;

        public string Explain(bool canonical)
        {
            return String.Join("\r\n\r\n",
                Directives.Select(d => d.Explain(canonical)).Concat(
                KnowledgeBase.Select(r => r.Explain(canonical)))
            );
        }

        public ErgoProgram(Directive[] directives, Predicate[] kb)
        {
            Directives = directives;
            KnowledgeBase = new KnowledgeBase();
            foreach (var k in kb) {
                KnowledgeBase.AssertZ(k);
            }
        }

        public static ErgoProgram Empty(Atom module) => new(
            new[] { new Directive(new Complex(new DefineModule().Signature.Functor, module, Literals.EmptyList)) }, 
            Array.Empty<Predicate>()
        );
    }

}
