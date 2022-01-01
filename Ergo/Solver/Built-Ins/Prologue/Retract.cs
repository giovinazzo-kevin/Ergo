using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Retract : DynamicPredicateBuiltIn
    {
        public Retract()
            : base("", new("retract"), Maybe.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var any = false;
            while (Retract(solver, scope, arguments[0], all: false))
            {
                yield return new(Literals.True);
                any = true;
            }
            if(!any)
            {
                yield return new(Literals.False);
            }
        }
    }
}
