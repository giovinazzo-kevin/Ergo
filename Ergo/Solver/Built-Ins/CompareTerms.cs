using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;

namespace Ergo.Solver.BuiltIns
{
    public sealed class CompareTerms : BuiltIn
    {
        public CompareTerms()
            : base("", new("@compare"), Maybe<int>.Some(3))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var cmp = (double)arguments[1].CompareTo(arguments[2]);
            if (arguments[0].IsGround)
            {
                if (!arguments[0].Matches<int>(out var result))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, arguments[0].Explain());
                }
                if (result.Equals(cmp))
                {
                    return new(Literals.True);
                }
                return new(Literals.False);
            }
            return new(Literals.True, new Substitution(arguments[0], new Atom(cmp)));
        }
    }
}
