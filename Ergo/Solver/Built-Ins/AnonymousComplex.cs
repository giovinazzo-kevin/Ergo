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
            if (!args[1].Matches<int>(out var arity))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, args[1].Explain());
            }
            if (args[0] is not Atom functor)
            {
                if (args[0].TryGetQualification(out var qm, out var qs) && qs is Atom functor_)
                {
                    var cplx = (ITerm)new Complex(functor_, Enumerable.Range(0, arity)
                        .Select(i => (ITerm)new Variable($"{i}"))
                        .ToArray());
                    if (cplx.TryQualify(qm, out var qualified))
                    {
                        return new(qualified);
                    }
                }
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Functor, args[0].Explain());
            }
            return new(new Complex(functor, Enumerable.Range(0, arity)
                .Select(i => (ITerm)new Variable($"{i}"))
                .ToArray()));
        }
    }
}
