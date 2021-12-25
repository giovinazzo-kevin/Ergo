using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Unify : BuiltIn
    {
        public Unify()
            : base("", new("@unify"), Maybe<int>.Some(2))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (new Substitution(arguments[0], arguments[1]).TryUnify(out var subs))
            {
                return new(Literals.True, subs.ToArray());
            }
            return new(Literals.False);
        }
    }
}
