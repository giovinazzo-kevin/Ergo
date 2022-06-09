using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Term : BuiltIn
    {
        public Term()
            : base("", new("term"), Maybe<int>.Some(3), Modules.Reflection)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var (functorArg, args, termArg) = (arguments[0], arguments[1], arguments[2]);
            if(termArg is not Variable)
            {
                if(termArg is Complex complex)
                {
                    if (!new Substitution(functorArg, complex.Functor).TryUnify(out var funSubs)
                    || !new Substitution(args, new List(complex.Arguments).Root).TryUnify(out var listSubs))
                    {
                        yield return new(WellKnown.Literals.False);
                        yield break;
                    }
                    yield return new(WellKnown.Literals.True, funSubs.Concat(listSubs).ToArray());
                    yield break;
                }
                if(termArg is Atom atom)
                {
                    if (!new Substitution(functorArg, atom).TryUnify(out var funSubs)
                    || !new Substitution(args, WellKnown.Literals.EmptyList).TryUnify(out var listSubs))
                    {
                        yield return new(WellKnown.Literals.False);
                        yield break;
                    }
                    yield return new(WellKnown.Literals.True, funSubs.Concat(listSubs).ToArray());
                    yield break;
                }
            }
            if (functorArg is Variable)
            {
                solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, functorArg.Explain()));
                yield return new(WellKnown.Literals.False);
                yield break;
            }
            if (functorArg is not Atom functor)
            {
                solver.Throw(new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Atom, functorArg.Explain()));
                yield return new(WellKnown.Literals.False);
                yield break;
            }
            if (!List.TryUnfold(args, out var argsList) || argsList.Contents.Length == 0)
            {
                if(args is not Variable && !args.Equals(WellKnown.Literals.EmptyList))
                {
                    solver.Throw(new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, args.Explain()));
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                if (!new Substitution(termArg, functor).TryUnify(out var subs)
                || !new Substitution(args, WellKnown.Literals.EmptyList).TryUnify(out var argsSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, subs.Concat(argsSubs).ToArray());
            }
            else
            {
                if (!new Substitution(termArg, new Complex(functor, argsList.Contents.ToArray())).TryUnify(out var subs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, subs.ToArray());
            }
        }
    }
}
