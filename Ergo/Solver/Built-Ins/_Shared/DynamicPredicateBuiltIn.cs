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

        pred = pred.Dynamic();
        if (scope.InterpreterScope.Modules[scope.InterpreterScope.Module].ContainsExport(pred.Head.GetSignature()))
            pred = pred.Exported();
        return pred.Qualified();
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
        var removed = 0;
        var sig = term.GetSignature();
        if (!term.IsQualified)
            term.TryQualify(scope.Module, out term);
        var toRemove = new List<ITerm>();
        foreach (var match in solver.KnowledgeBase.GetMatches(term, desugar: true))
        {
            if (!match.Rhs.IsDynamic)
            {
                scope.Throw(SolverError.CannotRetractStaticPredicate, scope, sig.Explain());
                return false;
            }

            if (scope.InterpreterScope.Module != match.Rhs.DeclaringModule)
            {
                scope.Throw(SolverError.CannotRetractImportedPredicate, scope, sig.Explain(), scope.InterpreterScope.Module.Explain(), match.Rhs.DeclaringModule.Explain());
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
