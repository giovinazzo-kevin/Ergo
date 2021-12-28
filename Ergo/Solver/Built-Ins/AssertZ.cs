using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AssertZ : DynamicPredicateBuiltIn
    {
        public AssertZ()
            : base("", new("assertz"), Maybe.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Assert(solver, scope, arguments[0], z: true))
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
