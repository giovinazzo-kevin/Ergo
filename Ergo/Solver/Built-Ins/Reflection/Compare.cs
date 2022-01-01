using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Compare : BuiltIn
    {
        public Compare()
            : base("", new("compare"), Maybe<int>.Some(3), Modules.Reflection)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var cmp = arguments[1].CompareTo(arguments[2]);
            if (arguments[0].IsGround)
            {
                if (!arguments[0].Matches<int>(out var result))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Number, arguments[0].Explain());
                }
                if (result.Equals(cmp))
                {
                    yield return new(Literals.True);
                }
                else
                {
                    yield return new(Literals.False);
                }
                yield break;
            }
            yield return new(Literals.True, new Substitution(arguments[0], new Atom(cmp)));
        }
    }
}
