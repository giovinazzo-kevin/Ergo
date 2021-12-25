using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Unprovable : BuiltIn
    {
        public Unprovable()
            : base("", new("@unprovable"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (solver.Solve(new Query(new(ImmutableArray<ITerm>.Empty.Add(arguments.Single()))), Maybe.Some(scope)).Any())
            {
                return new(Literals.False);
            }
            return new(Literals.True);
        }
    }
}
