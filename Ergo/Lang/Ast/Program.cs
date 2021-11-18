using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{


    public readonly struct Program
    {
        public readonly KnowledgeBase KnowledgeBank;

        public static string Explain(Program p)
        {
            return String.Join("\r\n\r\n", p.KnowledgeBank.Select(r => Predicate.Explain(r)));
        }

        public Program(params Predicate[] kb)
        {
            KnowledgeBank = new KnowledgeBase();
            foreach (var k in kb) {
                KnowledgeBank.AssertZ(k);
            }
        }

        public override string ToString()
        {
            return Explain(this);
        }
    }

}
