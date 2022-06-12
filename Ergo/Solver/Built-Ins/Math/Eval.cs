using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Eval : MathBuiltIn
    {
        public Eval()
            : base("", new("eval"), Maybe<int>.Some(1))
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if(solver.ShellScope.ExceptionHandler.TryGet(solver.ShellScope, () => new Evaluation(new Atom(Evaluate(arguments[0], solver.InterpreterScope))), out var value)) {
                yield return value;
            }
            yield return new Evaluation(WellKnown.Literals.False);
        }
    }
}
