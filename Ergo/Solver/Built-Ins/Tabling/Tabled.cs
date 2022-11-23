using Ergo.Interpreter.Libraries.Tabling;

namespace Ergo.Solver.BuiltIns;

public sealed class Tabled : SolverBuiltIn
{
    public Tabled()
        : base("(called by tabled predicates implicitly)", new("tabled"), 1, WellKnown.Modules.Tabling)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        /* tabled/1 overrides the regular SLD resolution with SLDT resolution.
         * Predicates tagged by the 'table' directive are rewritten as follows:
         * 
         *     p(_).
         *     p(X) :- q(X).
         *     p(X) :- r(X), s(X).
         *     
         * vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
         * 
         *     p(__K1) :- tabled(p__aux_(__K1)).
         *     
         *     p__aux_(_).
         *     p__aux_(X) :- q(X).
         *     p__aux_(X) :- r(X), s(X).
         *     
         * Therefore args[0] is the rewritten goal that should be memoized.
         */
        scope = scope.WithDepth(scope.Depth + 1);
        var memoContext = scope.InterpreterScope.GetLibrary<Tabling>(WellKnown.Modules.Tabling)
            .GetMemoizationContext(context);
        // The first call for a given tabled goal is dubbed the 'pioneer'.
        args[0].GetQualification(out var variant);
        if (!memoContext.GetPioneer(variant).TryGetValue(out var pioneer))
        {
            memoContext.MemoizePioneer(pioneer = variant);
            var any = false;
            foreach (var sol in context.Solve(new(args), scope))
            {
                any = true;
                memoContext.MemoizeSolution(pioneer, sol.Clone());
                yield return True(sol.Substitutions);
            }
            if (!any)
                yield return False();
        }
        // Subsequent variant calls are dubbed 'followers' of that pioneer. 
        else
        {
            var any = false;
            foreach (var sol in memoContext.GetSolutions(pioneer))
            {
                variant.Unify(pioneer.Substitute(sol.Substitutions)).TryGetValue(out var subs);
                yield return True(subs);
                any = true;
            }
            if (!any)
            {
                /* Backtracking is done similarly as in Prolog. When we backtrack to a tabled call,
                 * we use an alternative answer or a clause to resolve the call. After we exhaust all the
                 * answers and clauses, however, we cannot simply fail it since doing so we may risk losing
                 * answers. Instead, we decide whether it is necessary to re-execute the call starting from
                 * the first clause of the predicate. Re-execution will be repeated until no new answers can
                 * be generated, i.e., when the fixpoint is reached
                */
                //foreach(var sol in context.Solve(new(args), scope))
                //{
                //    context.MemoizeSolution(variant, sol);
                //    yield return True(sol.Substitutions);
                //    any = true;
                //}
                //if (!any)
                yield return False();
            }
        }
    }
}
