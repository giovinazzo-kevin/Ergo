using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Cut : BuiltIn
    {
        public Cut()
            : base("", new("@cut"), Maybe<int>.Some(0))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            return new(Literals.True);
        }
    }
}
