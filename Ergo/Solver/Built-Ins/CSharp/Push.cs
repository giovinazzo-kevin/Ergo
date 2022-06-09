using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Push : BuiltIn
    {
        public Push()
            : base("", new("push"), Maybe<int>.Some(1), Modules.CSharp)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            solver.PushData(args[0]);
            yield return new(WellKnown.Literals.True);
        }
    }
}
