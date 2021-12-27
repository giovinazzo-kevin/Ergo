using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ergo.Solver.BuiltIns
{

    public abstract class BuiltIn
    {
        public readonly Signature Signature;
        public readonly string Documentation;

        public abstract Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments);

        public BuiltIn(string documentation, Atom functor, Maybe<int> arity)
        {
            Signature = new(functor, arity, Maybe<Atom>.None);
            Documentation = documentation;
        }
    }
}
