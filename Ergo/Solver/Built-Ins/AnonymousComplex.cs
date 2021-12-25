using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AnonymousComplex : BuiltIn
    {
        public AnonymousComplex()
            : base("", new("@anon"), Maybe<int>.Some(2))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (!args[0].Matches<string>(out var functor))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Functor, args[0].Explain());
            }
            if (!args[1].Matches<int>(out var arity))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, args[1].Explain());
            }
            return new(new Complex(new(functor), Enumerable.Range(0, arity)
                .Select(i => (ITerm)new Variable($"{i}"))
                .ToArray()));
        }
    }
}
