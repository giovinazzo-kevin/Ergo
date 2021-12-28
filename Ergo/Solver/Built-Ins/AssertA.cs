using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AssertA : DynamicPredicateBuiltIn
    {
        public AssertA()
            : base("", new("asserta"), Maybe.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Assert(solver, scope, arguments[0], z: false)) return new(Literals.True);
            return new(Literals.False);
        }
    }
}
