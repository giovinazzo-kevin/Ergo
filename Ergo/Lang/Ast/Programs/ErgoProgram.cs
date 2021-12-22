using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{


    [DebuggerDisplay("{ Explain() }")]
    public readonly struct ErgoProgram
    {
        public readonly Directive[] Directives;
        public readonly KnowledgeBase KnowledgeBank;

        public string Explain()
        {
            return String.Join("\r\n\r\n",
                Directives.Select(d => d.Explain()).Concat(
                KnowledgeBank.Select(r => r.Explain()))
            );
        }

        public ErgoProgram(Directive[] directives, Predicate[] kb)
        {
            Directives = directives;
            KnowledgeBank = new KnowledgeBase();
            foreach (var k in kb) {
                KnowledgeBank.AssertZ(k);
            }
        }
    }

}
