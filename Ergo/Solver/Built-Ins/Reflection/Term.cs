using Ergo.Interpreter;
using Ergo.Lang.Exceptions;

namespace Ergo.Solver.BuiltIns;

public sealed class Term : BuiltIn
{
    public Term()
        : base("", new("term"), Maybe<int>.Some(3), Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var (functorArg, args, termArg) = (arguments[0], arguments[1], arguments[2]);
        if (termArg is not Variable)
        {
            if (termArg is Dict dict)
            {
                var tag = dict.Functor.Reduce<ITerm>(a => a, v => v);
                if (!functorArg.Unify(new Atom("dict")).TryGetValue(out var funSubs)
                || !args.Unify(new List(new[] { tag }.Append(new List(dict.KeyValuePairs).Root)).Root).TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, funSubs.Concat(listSubs).ToArray());
                yield break;
            }

            if (termArg is Complex complex)
            {
                if (!functorArg.Unify(complex.Functor).TryGetValue(out var funSubs)
                || !args.Unify(new List(complex.Arguments).Root).TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, funSubs.Concat(listSubs).ToArray());
                yield break;
            }

            if (termArg is Atom atom)
            {
                if (!functorArg.Unify(atom).TryGetValue(out var funSubs)
                || !args.Unify(WellKnown.Literals.EmptyList).TryGetValue(out var listSubs))
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
            if (args is not Variable && !args.Equals(WellKnown.Literals.EmptyList))
            {
                solver.Throw(new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, args.Explain()));
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            if (!termArg.Unify(functor).TryGetValue(out var subs)
            || !args.Unify(WellKnown.Literals.EmptyList).TryGetValue(out var argsSubs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.Concat(argsSubs).ToArray());
        }
        else
        {
            if (!termArg.Unify(new Complex(functor, argsList.Contents.ToArray())).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.ToArray());
        }
    }
}
