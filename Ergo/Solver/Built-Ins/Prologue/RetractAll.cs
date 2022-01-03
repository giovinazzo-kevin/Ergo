using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class RetractAll : DynamicPredicateBuiltIn
    {
        public RetractAll()
            : base("", new("retractall"), Maybe.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Retract(solver, scope, arguments[0], all: true)) yield return new(WellKnown.Literals.True);
            else yield return new(WellKnown.Literals.False);
        }
    }
}
