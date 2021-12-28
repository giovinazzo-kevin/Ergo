using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Eval1 : MathBuiltIn
    {
        public Eval1()
            : base("", new("@eval"), Maybe<int>.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            yield return new(new Atom(Eval(arguments[0])));
        }
    }
}
