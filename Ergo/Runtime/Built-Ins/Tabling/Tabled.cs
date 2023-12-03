using Ergo.Interpreter.Libraries.Tabling;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Tabled : BuiltIn
{
    private readonly Dictionary<ErgoVM, MemoizationContext> MemoContexts = new();

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
        var subs = SubstitutionMap.Pool.Acquire();
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
        SubstitutionMap.Pool.Release(subs);
        return nodes;
    }

    public override ErgoVM.Op Compile() => vm =>
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
        var args = vm.Args;
        if (!MemoContexts.TryGetValue(vm, out var memoContext))
            memoContext = MemoContexts[vm] = new MemoizationContext();
        // The first call for a given tabled goal is dubbed the 'pioneer'.
        args[0].GetQualification(out var variant);
        if (!memoContext.GetPioneer(variant).TryGetValue(out var pioneer))
        {
            memoContext.MemoizePioneer(pioneer = variant);
            var newVm = vm.Clone();
            newVm.Query = newVm.CompileQuery(new(args.ToImmutableArray()));
            newVm.Run();
            var any = false;
            foreach (var sol in newVm.Solutions)
            {
                any = true;
                memoContext.MemoizeSolution(pioneer, sol.Clone());
                vm.Solution(sol.Substitutions);
            }
            if (!any)
                vm.Fail();
        }
        // Subsequent variant calls are dubbed 'followers' of that pioneer. 
        else
        {
            var any = false;
            foreach (var sol in memoContext.GetSolutions(pioneer))
            {
                vm.SetArg(0, variant);
                vm.SetArg(1, pioneer.Substitute(sol.Substitutions));
                ErgoVM.Goals.Unify2(vm);
                vm.Solution();
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
                vm.Fail();
            }
        }
    };
}
