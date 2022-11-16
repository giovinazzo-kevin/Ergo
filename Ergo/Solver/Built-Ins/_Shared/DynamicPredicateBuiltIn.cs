namespace Ergo.Solver.BuiltIns;

public abstract class DynamicPredicateBuiltIn : SolverBuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Prologue)
    {
    }

    protected static Maybe<Predicate> GetPredicate(SolverScope scope, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, scope.InterpreterScope.Entry, out var pred))
        {
            scope.InterpreterScope.Throw(InterpreterError.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, arg.Explain());
            return default;
        }

        pred = pred.Dynamic();
        if (scope.InterpreterScope.Modules.TryGetValue(pred.DeclaringModule, out var declaringModule) && declaringModule.ContainsExport(pred.Head.GetSignature()))
            pred = pred.Exported();

        return pred.Qualified();
    }

    protected static bool Assert(ErgoSolver solver, SolverScope scope, ITerm arg, bool z)
    {
        if (!GetPredicate(scope, arg).TryGetValue(out var pred))
            return false;
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
        if (!term.IsQualified)
            term = term.Qualified(scope.Module);
        var toRemove = new List<ITerm>();
        foreach (var match in solver.KnowledgeBase.GetMatches(new("R"), term, desugar: true))
        {
            if (!match.Rhs.IsDynamic)
            {
                scope.Throw(SolverError.CannotRetractStaticPredicate, scope, sig.Explain());
                return false;
            }

            if (scope.InterpreterScope.Entry != match.Rhs.DeclaringModule)
            {
                scope.Throw(SolverError.CannotRetractImportedPredicate, scope, sig.Explain(), scope.InterpreterScope.Entry.Explain(), match.Rhs.DeclaringModule.Explain());
                return false;
            }

            toRemove.Add(match.Rhs.Head);

            if (!all)
                break;

        }

        foreach (var item in toRemove)
            solver.KnowledgeBase.Retract(item);

        return toRemove.Count > 0;
    }
}
