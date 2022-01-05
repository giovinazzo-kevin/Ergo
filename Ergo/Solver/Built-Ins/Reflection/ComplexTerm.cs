using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class ComplexTerm : BuiltIn
    {
        public ComplexTerm()
            : base("", new("complex"), Maybe<int>.Some(3), Modules.Reflection)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var (functorArg, args, complexArg) = (arguments[0], arguments[1], arguments[2]);
            if(complexArg is not Variable)
            {
                if(complexArg is not Complex complex)
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Complex, complexArg.Explain());
                }
                if(!new Substitution(functorArg, complex.Functor).TryUnify(out var funSubs)
                || !new Substitution(args, new List(complex.Arguments).Root).TryUnify(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, funSubs.Concat(listSubs).ToArray());
                yield break;
            }
            if (functorArg is Variable)
            {
                throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, functorArg.Explain());
            }
            if (args is Variable)
            {
                throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, args.Explain());
            }
            if (functorArg is not Atom functor)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Atom, functorArg.Explain());
            }
            if(!List.TryUnfold(args, out var argsList))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, args.Explain());
            }
            if (!new Substitution(complexArg, new Complex(functor, argsList.Contents.ToArray())).TryUnify(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }
            yield return new(WellKnown.Literals.True, subs.ToArray());
        }
    }
}
