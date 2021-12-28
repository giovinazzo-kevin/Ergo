using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Cut : BuiltIn
    {
        public Cut()
            : base("", new("!"), Maybe<int>.Some(0))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            yield return new(Literals.True);
        }
    }
}
