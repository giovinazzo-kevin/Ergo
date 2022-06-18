﻿using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public abstract class DynamicPredicateBuiltIn : BuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, Modules.Prologue)
    {
    }

    protected static Predicate GetPredicate(ErgoSolver solver, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, solver.InterpreterScope.Module, out var pred))
        {
            throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Predicate, arg.Explain());
        }

        pred = pred.Qualified().Dynamic();
        return pred;
    }

    protected static bool Assert(ErgoSolver solver, ITerm arg, bool z)
    {
        var pred = GetPredicate(solver, arg);
        if (!solver.Interpreter.TryAddDynamicPredicate(new(pred.Head.GetSignature(), pred, assertz: z)))
        {
            return false;
        }

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
        if (!solver.Interpreter.DynamicPredicates.TryGetValue(sig, out var dynPreds))
        {
            if (!term.IsQualified && term.TryQualify(scope.Module, out term) && !solver.Interpreter.DynamicPredicates.TryGetValue(term.GetSignature(), out dynPreds))
            {
                return false;
            }
        }

        var removed = 0;
        foreach (var dyn in dynPreds)
        {
            if (solver.InterpreterScope.Module != dyn.Predicate.DeclaringModule)
            {
                throw new SolverException(SolverError.CannotRetractImportedPredicate, scope, sig.Explain(), solver.InterpreterScope.Module.Explain(), dyn.Predicate.DeclaringModule.Explain());
            }

            solver.Interpreter.TryRemoveDynamicPredicate(dyn);
            solver.KnowledgeBase.Retract(dyn.Predicate.Head);
            if (!all)
            {
                return true;
            }

            ++removed;
        }

        return removed > 0;
    }
}
