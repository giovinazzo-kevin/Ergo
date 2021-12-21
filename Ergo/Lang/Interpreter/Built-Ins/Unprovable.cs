using System;
using System.Linq;

namespace Ergo.Lang
{
    public sealed class Unprovable : BuiltIn
    {
        public Unprovable()
            : base("", new("@unprovable"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            if (solver.Solve(new Query(new(arguments.Single())), Maybe.Some(scope)).Any())
            {
                return new(Literals.False);
            }
            return new(Literals.True);
        }
    }
}
