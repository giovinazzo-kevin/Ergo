using Ergo.Lang.Ast;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ergo.Lang
{

    public abstract class BuiltIn
    {
        public readonly BuiltInSignature Signature;
        public readonly string Documentation;

        public abstract Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments);

        public BuiltIn(string documentation, Atom functor, Maybe<int> arity)
        {
            Signature = new(functor, arity);
            Documentation = documentation;
        }
    }
}
