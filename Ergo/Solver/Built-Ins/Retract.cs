using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Retract : DynamicPredicateBuiltIn
    {
        public Retract()
            : base("", new("retract"), Maybe.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Retract(solver, scope, arguments[0], all: false)) return new(Literals.True);
            return new(Literals.False);
        }
    }
}
