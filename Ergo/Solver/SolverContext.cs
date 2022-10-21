using System.Runtime.ExceptionServices;

namespace Ergo.Solver;

// TODO: Build a stack-based system that supports last call optimization
public sealed class SolverContext
{
    private readonly CancellationTokenSource ChoicePointCts = new();
    private readonly CancellationTokenSource ExceptionCts = new();
    public readonly ErgoSolver Solver;

    internal SolverContext(ErgoSolver solver) => Solver = solver;

    public async IAsyncEnumerable<Solution> Solve(Query goal, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        scope.InterpreterScope.ExceptionHandler.Throwing += Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught += Cancel;
        await foreach (var s in Solve(goal.Goals, scope, ct: ct))
        {
            yield return s;
        }
        scope.InterpreterScope.ExceptionHandler.Throwing -= Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught -= Cancel;
        void Cancel(ExceptionDispatchInfo _) => ExceptionCts.Cancel(false);
    }

    private async IAsyncEnumerable<Solution> Solve(NTuple query, SolverScope scope, List<Substitution> subs = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        subs ??= new List<Substitution>();
        if (query.IsEmpty)
        {
            yield return Solution.Success(scope, subs.ToArray());
            yield break;
        }

        // This method takes a list of goals and solves them one at a time.
        // The tail of the list is fed back into this method recursively.
        var goals = query.Contents;
        var subGoal = goals.First();
        goals = goals.RemoveAt(0);

        // Get first solution for the current subgoal
        await foreach (var s in Solve(subGoal, scope, subs, ct: ct))
        {
            // Solve the rest of the goal 
            var rest = new NTuple(goals.Select(x => x.Substitute(s.Substitutions)));
            await foreach (var ss in Solve(rest, s.Scope, subs, ct: ct))
            {
                yield return Solution.Success(ss.Scope, s.Substitutions.Concat(ss.Substitutions).Distinct().ToArray());
            }
        }
    }


    private async IAsyncEnumerable<Solution> Solve(ITerm goal, SolverScope scope, List<Substitution> subs = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct = CancellationTokenSource.CreateLinkedTokenSource(ct, ChoicePointCts.Token, ExceptionCts.Token).Token;
        if (ct.IsCancellationRequested) yield break;

        if (goal.IsParenthesized)
            scope = scope.WithoutCut();

        subs ??= new List<Substitution>();
        // || WellKnown.Functors.Disjunction.Contains(goal.GetFunctor().GetOr(default))
        // Treat comma-expression complex ITerms as proper expressions
        if (NTuple.FromPseudoCanonical(goal, default, default).TryGetValue(out var expr))
        {
            await foreach (var s in Solve(expr, scope, subs, ct: ct))
            {
                yield return s;
            }

            yield break;
        }

        // If a goal is expanded, all of its possible expansions are enumerated.
        // If a goal has no expansions, it is returned as-is.
        await foreach (var exp in Solver.ExpandTerm(goal, scope, ct: ct))
        {
            if (ct.IsCancellationRequested) yield break;
            // If goal resolves to a builtin, it is called on the spot and its solutions enumerated (usually just ⊤ or ⊥, plus a list of substitutions)
            // If goal does not resolve to a builtin it is returned as-is, and it is then matched against the knowledge base.
            await foreach (var resolvedGoal in Solver.ResolveGoal(exp, scope, ct: ct))
            {
                if (ct.IsCancellationRequested) yield break;
                if (resolvedGoal.Result.Equals(WellKnown.Literals.False) || resolvedGoal.Result is Variable)
                {
                    Solver.LogTrace(SolverTraceType.BuiltInResolution, "⊥", scope.Depth);
                    if (scope.IsCutRequested)
                        ChoicePointCts.Cancel(false);
                    yield break;
                }

                if (resolvedGoal.Result.Equals(WellKnown.Literals.True))
                {
                    // Solver.LogTrace(SolverTraceType.Return, $"⊤ {{{string.Join("; ", subs.Select(s => s.Explain()))}}}", Scope.Depth);
                    if (exp.Equals(WellKnown.Literals.Cut))
                        scope = scope.WithCut();

                    yield return Solution.Success(scope, subs.Concat(resolvedGoal.Substitutions).ToArray());
                    if (scope.IsCutRequested)
                        ChoicePointCts.Cancel(false);
                    continue;
                }

                // Attempts qualifying a goal with a module, then finds matches in the knowledge base
                var anyQualified = false;
                foreach (var (qualifiedGoal, isDynamic) in ErgoSolver.GetImplicitGoalQualifications(resolvedGoal.Result, scope))
                {
                    if (ct.IsCancellationRequested) yield break;
                    var matches = Solver.KnowledgeBase.GetMatches(qualifiedGoal, desugar: false);
                    foreach (var m in matches)
                    {
                        anyQualified = true;
                        // Create a new scope and context for this procedure call, essentially creating a choice point
                        var innerScope = scope
                            .WithDepth(scope.Depth + 1)
                            .WithModule(m.Rhs.DeclaringModule)
                            .WithCallee(scope.Callee)
                            .WithCaller(m.Rhs);
                        var innerContext = new SolverContext(Solver);
                        Solver.LogTrace(SolverTraceType.Call, m.Lhs, scope.Depth);

                        /* https://sicstus.sics.se/sicstus/docs/4.0.1/html/sicstus/Last-Call-Optimization.html
                            Another important efficiency feature of SICStus Prolog is last call optimization. 
                            This is a space optimization technique, which applies when a predicate is determinate 
                              at the point where it is about to call the last goal in the body of a clause.

                             % for(Int, Lower, Upper)
                             % Lower and Upper should be integers such that Lower =< Upper.
                             % Int should be uninstantiated; it will be bound successively on
                             % backtracking to Lower, Lower+1, ... Upper.
     
                             for(Int, Int, _Upper).
                             for(Int, Lower, Upper) :-
                                for(Int, Next, Upper).
                                Lower < Upper,
                                Next is Lower + 1,
                                for(Int, Next, Upper).

                            This predicate is determinate at the point where the recursive call is about to be made, 
                              since this is the last clause and the preceding goals (<)/2 and is/2) are determinate. 
                            Thus last call optimization can be applied; effectively, the stack space being used for
                              the current predicate call is reclaimed before the recursive call is made. 

                            This means that this predicate uses only a constant amount of space, no matter how deep the recursion.
                         */


                        var solve = innerContext.Solve(m.Rhs.Body, innerScope, new List<Substitution>(m.Substitutions.Concat(resolvedGoal.Substitutions)), ct: ct);
                        await foreach (var s in solve)
                        {
                            Solver.LogTrace(SolverTraceType.Exit, m.Rhs.Head, s.Scope.Depth);
                            yield return s;
                        }
                        if (innerContext.ChoicePointCts.IsCancellationRequested)
                            break;
                        if (innerContext.ExceptionCts.IsCancellationRequested)
                            ExceptionCts.Cancel(false);

                    }
                    if (anyQualified)
                        break;
                }

                if (!anyQualified)
                {
                    var signature = resolvedGoal.Result.GetSignature();
                    var dyn = scope.InterpreterScope.Modules.Values
                        .SelectMany(m => m.DynamicPredicates)
                        .SelectMany(p => new[] { p, p.WithModule(default) })
                        .ToHashSet();
                    if (dyn.Contains(signature))
                        continue;
                    if (Solver.Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                    {
                        scope.Throw(SolverError.UndefinedPredicate, signature.Explain());
                        yield break;
                    }
                }
            }
        }

    }
}
