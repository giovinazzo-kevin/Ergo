namespace Ergo.Solver;

public sealed class SolverContext
{
    private readonly CancellationTokenSource ExceptionCts = new();
    public readonly ErgoSolver Solver;
    public SolverScope Scope { get; private set; }

    public SolverContext(ErgoSolver solver, SolverScope scope)
    {
        Solver = solver;
        Scope = scope;

        Solver.Throwing += _ => ExceptionCts.Cancel(false);
    }

    public IAsyncEnumerable<Solution> Solve(Query goal, CancellationToken ct = default) => Solve(goal.Goals, ct: ct);

    private async IAsyncEnumerable<Solution> Solve(NTuple query, List<Substitution> subs = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        subs ??= new List<Substitution>();
        if (query.IsEmpty)
        {
            yield return new Solution(subs.ToArray());
            yield break;
        }

        var goals = query.Contents;
        var subGoal = goals.First();
        goals = goals.RemoveAt(0);
        Scope = Scope.WithChoicePoint();
        // Get first solution for the current subgoal
        await foreach (var s in Solve(subGoal, subs, ct: ct))
        {
            var rest = new NTuple(goals.Select(x => x.Substitute(s.Substitutions)));
            await foreach (var ss in Solve(rest, subs, ct: ct))
            {
                yield return new Solution(s.Substitutions.Concat(ss.Substitutions).Distinct().ToArray());
            }

            if (Scope.IsCutRequested)
            {
                yield break;
            }
        }
    }

    private async IAsyncEnumerable<Solution> Solve(ITerm goal, List<Substitution> subs = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct = CancellationTokenSource.CreateLinkedTokenSource(ct, ExceptionCts.Token).Token;

        if (ct.IsCancellationRequested)
        {
            yield break;
        }

        subs ??= new List<Substitution>();
        // Treat comma-expression complex ITerms as proper expressions
        if (NTuple.TryUnfold(goal, out var expr))
        {
            await foreach (var s in Solve(new NTuple(expr), subs, ct: ct))
            {
                yield return s;
            }

            Scope = Scope.WithChoicePoint();
            yield break;
        }
        // If goal resolves to a builtin, it is called on the spot and its solutions enumerated (usually just ⊤ or ⊥, plus a list of substitutions)
        // If goal does not resolve to a builtin it is returned as-is, and it is then matched against the knowledge base.
        await foreach (var resolvedGoal in Solver.ResolveGoal(goal, Scope, ct: ct))
        {
            if (ct.IsCancellationRequested)
            {
                yield break;
            }

            if (resolvedGoal.Result.Equals(WellKnown.Literals.False) || resolvedGoal.Result is Variable)
            {
                Solver.LogTrace(SolverTraceType.Retn, "⊥", Scope.Depth);
                yield break;
            }

            if (resolvedGoal.Result.Equals(WellKnown.Literals.True))
            {
                Solver.LogTrace(SolverTraceType.Retn, $"⊤ {{{string.Join("; ", subs.Select(s => s.Explain()))}}}", Scope.Depth);
                yield return new Solution(subs.Concat(resolvedGoal.Substitutions).ToArray());
                if (goal.Equals(WellKnown.Literals.Cut))
                {
                    Scope.Cut();
                    yield break;
                }

                continue;
            }

            var expandedGoal = resolvedGoal.Result;
            // Cyclic literal definitions throw an error, so this replacement loop always terminates
            while (await Solver.TryExpandTerm(expandedGoal, Scope, ct: ct) is { HasValue: true } exp)
            {
                if (ct.IsCancellationRequested)
                {
                    yield break;
                }

                var newGoal = exp.GetOrDefault();
                if (newGoal.Unify(expandedGoal).Reduce(some => !some.Any(), () => false))
                    break;
                expandedGoal = newGoal;
            }
            // Attempts qualifying a goal with a module, then finds matches in the knowledge base
            var (qualifiedGoal, matches) = Solver.QualifyGoal(Scope, expandedGoal);
            Solver.LogTrace(SolverTraceType.Call, qualifiedGoal, Scope.Depth);
            foreach (var m in matches)
            {
                var innerScope = Scope.WithDepth(Scope.Depth + 1)
                    .WithModule(m.Rhs.DeclaringModule)
                    .WithCallee(Scope.Callee)
                    .WithCaller(m.Rhs);
                var innerContext = new SolverContext(Solver, innerScope);
                var solve = innerContext.Solve(m.Rhs.Body, new List<Substitution>(m.Substitutions.Concat(resolvedGoal.Substitutions)), ct: ct);
                await foreach (var s in solve)
                {
                    Solver.LogTrace(SolverTraceType.Exit, m.Rhs.Head, innerScope.Depth);
                    yield return s;
                }

                if (innerScope.IsCutRequested)
                {
                    Scope = Scope.WithChoicePoint();
                    yield break;
                }
            }
        }
    }
}
