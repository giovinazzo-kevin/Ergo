using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Write : BuiltIn
    {
        public Write()
            : base("", new("write"), Maybe<int>.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if(CommaSequence.TryUnfold(args[0], out var comma))
            {
                Console.Write(String.Join(String.Empty, comma.Contents.Select(x => x.Explain())));
            }
            else
            {
                Console.Write(args[0].Explain());
            }
            yield return new(Literals.True);
        }
    }
}
