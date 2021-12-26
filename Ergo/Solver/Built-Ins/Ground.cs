using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Ground : BuiltIn
    {
        public Ground()
            : base("", new("@ground"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            return new(new Lang.Ast.Atom(arguments[0].IsGround));
        }
    }
}
