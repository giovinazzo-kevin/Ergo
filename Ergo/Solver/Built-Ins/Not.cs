using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Lang.Exceptions;
using System.Linq;
using Ergo.Lang;
using Ergo.Interpreter;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Not : BuiltIn
    {
        public Not()
            : base("", new("@not"), Maybe<int>.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var arg = arguments.Single();
            if (!arg.Matches<bool>(out var eval))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Boolean, arg.Explain());
            }
            yield return new(new Atom(!eval));
        }
    }
}
