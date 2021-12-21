using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Directive
    {
        public readonly ITerm Body;
        public readonly string Documentation;

        public Directive(string doc, ITerm body)
        {
            Documentation = doc;
            Body = body; 
        }

        public Directive WithBody(ITerm newBody) => new(Documentation, newBody);

        public string Explain()
        {
            var expl = $":- {Body.Explain()}.";
            if (!String.IsNullOrWhiteSpace(Documentation))
            {
                expl = $"{String.Join("\r\n", Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r))}\r\n" + expl;
            }

            return expl;
        }
    }
}
