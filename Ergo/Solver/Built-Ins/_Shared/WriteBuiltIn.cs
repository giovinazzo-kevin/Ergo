using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public abstract class WriteBuiltIn : BuiltIn
    {
        public readonly bool Canonical;

        protected WriteBuiltIn(string documentation, Atom functor, Maybe<int> arity, bool canon) 
            : base(documentation, functor, arity)
        {
            Canonical = canon;
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (CommaSequence.TryUnfold(args[0], out var comma))
            {
                Console.Write(String.Join(String.Empty, comma.Contents.Select(x => x.Explain(canonical: Canonical))));
            }
            else
            {
                Console.Write(args[0].Explain(Canonical));
            }
            yield return new(Literals.True);
        }
    }
}
