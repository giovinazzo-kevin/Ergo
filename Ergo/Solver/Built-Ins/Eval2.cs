using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Eval2 : MathBuiltIn
    {
        public Eval2()
            : base("", new("@eval"), Maybe<int>.Some(2))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var result = new Atom(Eval(arguments[1], solver.InterpreterScope));
            if (new Substitution(arguments[0], result).TryUnify(out var subs)) {
                yield return new(Literals.True, subs.ToArray());
            }
            else
            {
                yield return new(Literals.False);
            }
        }
    }
}
