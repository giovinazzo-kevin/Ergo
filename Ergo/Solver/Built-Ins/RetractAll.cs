using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver.BuiltIns
{
    public sealed class RetractAll : DynamicPredicateBuiltIn
    {
        public RetractAll()
            : base("", new("retractall"), Maybe.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Retract(solver, scope, arguments[0], all: true)) return new(Literals.True);
            return new(Literals.False);
        }
    }
}
