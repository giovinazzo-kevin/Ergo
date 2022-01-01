using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Not : BuiltIn
    {
        public Not()
            : base("", new("not"), Maybe<int>.Some(1), Modules.Prologue)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (solver.Solve(new Query(new(ImmutableArray<ITerm>.Empty.Add(arguments.Single()))), Maybe.Some(scope)).Any())
            {
                yield return new(Literals.False);
            }
            else yield return new(Literals.True);
        }
    }
}
