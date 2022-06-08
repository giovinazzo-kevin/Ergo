using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class CommaList : BuiltIn
    {
        public CommaList()
            : base("", new("comma_list"), Maybe<int>.Some(2), Modules.Reflection)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var (commaArg, listArg) = (arguments[0], arguments[1]);
            if(listArg is not Variable)
            {
                if(!List.TryUnfold(listArg, out var list))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, listArg.Explain());
                }
                var comma = new CommaSequence(list.Contents);
                if(!new Substitution(commaArg, comma.Root).TryUnify(out var subs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
            if(commaArg is not Variable)
            {
                if (!CommaSequence.TryUnfold(commaArg, out var comma))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.CommaSequence, commaArg.Explain());
                }
                var list = new List(comma.Contents);
                if (!new Substitution(listArg, list.Root).TryUnify(out var subs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
            throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, commaArg.Explain());
        }
    }
}
