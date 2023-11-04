using Ergo.Interpreter.Libraries.Tabling;
using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Tabled : SolverBuiltIn
{
    public override int OptimizationOrder => base.OptimizationOrder;

    public Tabled()
        : base("(called by tabled predicates implicitly)", new("tabled"), 1, WellKnown.Modules.Tabling)
    {
    }

    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        // If there are multiple calls to the same variant of a tabled predicate, they can be coalesced into one call.
        // This requires removing all redundant calls after the first, and replacing all referenced variables with variables from the pioneer.
        var tabledCalls = nodes
            .OfType<BuiltInNode>()
            .Where(x => x.BuiltIn is Tabled)
            .GroupBy(x => x.Goal.NumberVars())
            .Where(g => g.Count() > 1)
            .Select(g => (Pioneer: g.First(), Followers: g.Skip(1).ToArray()))
            .ToArray();
        var subs = Substitution.Pool.Acquire();
        foreach (var (pioneer, followers) in tabledCalls)
        {
            foreach (var follower in followers)
            {
                nodes.Remove(follower);
                if (pioneer.Goal.Unify(follower.Goal).TryGetValue(out var innerSubs))
                    SubstitutionMap.MergeRef(subs, innerSubs);
            }
        }
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i] = nodes[i].Substitute(subs);
        }
        Substitution.Pool.Release(subs);
        return nodes;
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
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
                LanguageExtensions.Unify(variant, pioneer.Substitute(sol.Substitutions)).TryGetValue(out var subs);
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
