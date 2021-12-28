using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AssertA : DynamicPredicateBuiltIn
    {
        public AssertA()
            : base("", new("asserta"), Maybe.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Assert(solver, scope, arguments[0], z: false))
            {
                yield return new(Literals.True);
            }
            else
            {
                yield return new(Literals.False);
            }
        }
    }
}
