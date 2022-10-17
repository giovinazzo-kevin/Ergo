using System.Runtime.ExceptionServices;

namespace Ergo.Solver;

public sealed class SolverContext
{
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
            //if (s.Scope.IsCutRequested)
            //    break;
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
                // Handle cuts
                if (ss.Scope.IsCutRequested)
                {
                    yield break;
                }
            }
        }
    }


    private async IAsyncEnumerable<Solution> Solve(ITerm goal, SolverScope scope, List<Substitution> subs = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct = CancellationTokenSource.CreateLinkedTokenSource(ct, ExceptionCts.Token).Token;
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
                        ExceptionCts.Cancel(false);
                    yield break;
                }

                if (resolvedGoal.Result.Equals(WellKnown.Literals.True))
                {
                    // Solver.LogTrace(SolverTraceType.Return, $"⊤ {{{string.Join("; ", subs.Select(s => s.Explain()))}}}", Scope.Depth);
                    if (exp.Equals(WellKnown.Literals.Cut))
                        scope = scope.WithCut();

                    yield return Solution.Success(scope, subs.Concat(resolvedGoal.Substitutions).ToArray());
                    continue;
                }

                // Attempts qualifying a goal with a module, then finds matches in the knowledge base
                var anyQualified = false;
                foreach (var (qualifiedGoal, isDynamic) in ErgoSolver.GetImplicitGoalQualifications(resolvedGoal.Result, scope))
                {
                    if (ct.IsCancellationRequested) yield break;
                    Solver.LogTrace(SolverTraceType.Call, qualifiedGoal, scope.Depth);
                    var matches = Solver.KnowledgeBase.GetMatches(qualifiedGoal, desugar: false);
                    foreach (var m in matches)
                    {
                        anyQualified = true;
                        var innerScope = scope
                            .WithDepth(scope.Depth + 1)
                            .WithModule(m.Rhs.DeclaringModule)
                            .WithCallee(scope.Callee)
                            .WithCaller(m.Rhs)
                            ;
                        var solve = Solve(m.Rhs.Body, innerScope, new List<Substitution>(m.Substitutions.Concat(resolvedGoal.Substitutions)), ct: ct);
                        await foreach (var s in solve)
                        {
                            Solver.LogTrace(SolverTraceType.Exit, m.Rhs.Head, s.Scope.Depth);
                            yield return s;
                        }

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
