using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;

namespace Ergo.Solver.BuiltIns
{
    public abstract class DynamicPredicateBuiltIn : BuiltIn
    {
        protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
            : base(documentation, functor, arity)
        {
        }

        protected bool Assert(ErgoSolver solver, SolverScope scope, ITerm term, bool z)
        {
            if (!Predicate.TryUnfold(term, scope.Module, out var pred))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Predicate, term.Explain());
            }
            pred = pred.Qualified().AsDynamic().WithModuleName(scope.Module);
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

        protected bool Retract(ErgoSolver solver, SolverScope scope, ITerm term, bool all)
        {
            var sig = term.GetSignature();
            if (!solver.Interpreter.DynamicPredicates.TryGetValue(sig, out var dynPreds))
                return false;
            foreach (var dyn in dynPreds)
            {
                if (solver.InterpreterScope.CurrentModule != dyn.Predicate.DeclaringModule)
                {
                    throw new InterpreterException(InterpreterError.CannotRetractImportedPredicate, sig.Explain(), solver.InterpreterScope.CurrentModule.Explain(), dyn.Predicate.DeclaringModule.Explain());
                }
                solver.Interpreter.TryRemoveDynamicPredicate(dyn);
                if(!all)
                {
                    return true;
                }
            }
            return true;
        }
    }
}
