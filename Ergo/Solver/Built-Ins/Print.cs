using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Print : BuiltIn
    {
        public Print()
            : base("", new("@print"), Maybe<int>.None)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            foreach (var arg in args)
            {
                Console.Write(arg.Explain());
            }
            yield return new(Literals.True);
        }
    }
}
