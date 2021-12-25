using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Directive
    {
        public readonly ITerm Body;

        public Directive(ITerm body)
        {
            Body = body; 
        }

        public Directive WithBody(ITerm newBody) => new(newBody);

        public string Explain() => $":- {Body.Explain()}.";
    }
}
