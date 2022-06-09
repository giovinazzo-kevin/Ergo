using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Pull : BuiltIn
    {
        public Pull()
            : base("", new("pull_data"), Maybe<int>.Some(1), Modules.CSharp)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            var any = false;
            await foreach(var item in solver.GetDataSourceMatches(args[0]))
            {
                if(Predicate.TryUnify(args[0], item.Rhs, out var subs))
                {
                    any = true;
                    yield return new(WellKnown.Literals.True, subs.ToArray());
                }
            }
            if(!any)
            {
                yield return new(WellKnown.Literals.False);
            }
        }
    }
}
