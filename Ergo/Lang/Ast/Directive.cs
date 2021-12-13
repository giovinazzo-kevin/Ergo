using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Directive
    {
        public readonly Term Body;
        public readonly string Documentation;

        public Directive(string doc, Term body)
        {
            Documentation = doc;
            Body = body; 
        }

        public Directive WithBody(Term newBody) => new(Documentation, newBody);

        public static string Explain(Directive d)
        {
            var expl = $":- {Term.Explain(d.Body)}.";
            if (!String.IsNullOrWhiteSpace(d.Documentation))
            {
                expl = $"{String.Join("\r\n", d.Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r))}\r\n" + expl;
            }

            return expl;
        }
    }
}
