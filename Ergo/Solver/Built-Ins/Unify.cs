using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Unify : BuiltIn
    {
        public Unify()
            : base("", new("@unify"), Maybe<int>.Some(2), Modules.Prologue)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (new Substitution(arguments[0], arguments[1]).TryUnify(out var subs))
            {
                yield return new(Literals.True, subs.ToArray());
            }
            else yield return new(Literals.False);
        }
    }
}
