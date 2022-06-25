using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public abstract class DynamicPredicateBuiltIn : SolverBuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Prologue)
    {
    }

    protected static Predicate GetPredicate(SolverScope scope, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, scope.InterpreterScope.Module, out var pred))
        {
            throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope.InterpreterScope, WellKnown.Types.Predicate, arg.Explain());
        }

        pred = pred.Qualified().Dynamic();
        return pred;
    }

    protected static bool Assert(ErgoSolver solver, SolverScope scope, ITerm arg, bool z)
    {
        var pred = GetPredicate(scope, arg);
        if (!z)
        {
            solver.KnowledgeBase.AssertA(pred);
        }
        else
        {
            solver.KnowledgeBase.AssertZ(pred);
        }

        return true;
    }

    protected static bool Retract(ErgoSolver solver, SolverScope scope, ITerm term, bool all)
    {
        var sig = term.GetSignature();
        if (!solver.KnowledgeBase.TryGet(sig, out var preds))
            return false;

        var removed = 0;
        foreach (var dyn in preds.ToHashSet())
        {
            if (!dyn.IsDynamic)
            {
                scope.Throw(SolverError.CannotRetractStaticPredicate, scope, sig.Explain());
                return false;
            }

            if (scope.InterpreterScope.Module != dyn.DeclaringModule)
            {
                scope.Throw(SolverError.CannotRetractImportedPredicate, scope, sig.Explain(), scope.InterpreterScope.Module.Explain(), dyn.DeclaringModule.Explain());
                return false;
            }

            if (!dyn.Head.Unify(term).HasValue)
                continue;

            preds.Remove(dyn);
            if (!all)
            {
                return true;
            }

            ++removed;
        }

        return removed > 0;
    }
}
