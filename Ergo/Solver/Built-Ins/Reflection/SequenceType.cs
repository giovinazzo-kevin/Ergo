using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class SequenceType : BuiltIn
    {
        public SequenceType()
            : base("", new("seq_type"), Maybe<int>.Some(2), Modules.Reflection)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var (type, seq) = (arguments[0], arguments[1]);
            if (seq is Variable)
            {
                solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, seq.Explain()));
                yield break;
            }
            if (List.TryUnfold(seq, out _))
            {
                if (new Substitution(type, new Atom("list")).TryUnify(out var subs))
                {
                    yield return new(WellKnown.Literals.True, subs.ToArray());
                    yield break;
                }
            }
            if (CommaSequence.TryUnfold(seq, out _))
            {
                if (new Substitution(type, new Atom("comma")).TryUnify(out var subs))
                {
                    yield return new(WellKnown.Literals.True, subs.ToArray());
                    yield break;
                }
            }
            yield return new(WellKnown.Literals.False);
        }
    }
}
