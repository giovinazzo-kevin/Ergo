using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Lang.Exceptions;
using System.Linq;
using Ergo.Lang;
using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Not : BuiltIn
    {
        public Not()
            : base("", new("@not"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var arg = arguments.Single();
            if (!arg.Matches<bool>(out var eval))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Boolean, arg.Explain());
            }
            return new(new Atom(!eval));
        }
    }
}
