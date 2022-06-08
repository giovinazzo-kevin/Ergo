using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Is : MathBuiltIn
    {
        public Is()
            : base("", new("is"), Maybe<int>.Some(2))
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var result = new Atom(Evaluate(arguments[1], solver.InterpreterScope));
            if (new Substitution(arguments[0], result).TryUnify(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs.ToArray());
            }
            else
            {
                yield return new(WellKnown.Literals.False);
            }
        }
    }
}
