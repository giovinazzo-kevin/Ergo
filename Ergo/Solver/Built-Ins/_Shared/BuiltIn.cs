using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{

    public abstract class BuiltIn
    {
        public readonly Signature Signature;
        public readonly string Documentation;

        public Predicate GetStub(ITerm[] arguments)
        {
            var head = new Complex(Signature.Functor, arguments);
            return new Predicate(Documentation, Signature.Module.Reduce(some => some, () => Modules.Stdlib), head, CommaSequence.Empty, dynamic: false);
        }

        public abstract IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments);

        public BuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
        {
            Signature = new(functor, arity, Maybe.Some(module), Maybe<Atom>.None);
            Documentation = documentation;
        }
    }
}
