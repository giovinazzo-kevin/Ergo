using Ergo.Interpreter;
using Ergo.Lang.Exceptions;

namespace Ergo.Solver.BuiltIns;

public sealed class CommaList : BuiltIn
{
    public CommaList()
        : base("", new("comma_list"), Maybe<int>.Some(2), Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var (commaArg, listArg) = (arguments[0], arguments[1]);
        if (listArg is not Variable)
        {
            if (!List.TryUnfold(listArg, out var list))
            {
                solver.Throw(new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, listArg.Explain()));
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            var comma = new CommaSequence(list.Contents);
            if (!commaArg.Unify(comma.Root).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.ToArray());
            yield break;
        }

        if (commaArg is not Variable)
        {
            if (!CommaSequence.TryUnfold(commaArg, out var comma))
            {
                solver.Throw(new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.CommaSequence, commaArg.Explain()));
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            var list = new List(comma.Contents);
            if (!listArg.Unify(list.Root).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.ToArray());
            yield break;
        }

        solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, commaArg.Explain()));
        yield return new(WellKnown.Literals.False);
    }
}
