using Ergo.Events.Solver;
using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;
using System.Runtime.ExceptionServices;

namespace Ergo.Solver;

// TODO: Build a stack-based system that supports last call optimization
public sealed class SolverContext : IDisposable
{
    public readonly SolverContext Parent;

    private readonly CancellationTokenSource ChoicePointCts = new();
    private readonly CancellationTokenSource ExceptionCts = new();

    public readonly InterpreterScope Scope;
    public readonly ErgoSolver Solver;

    private SolverContext(ErgoSolver solver, InterpreterScope scope, SolverContext parent)
    {
        Parent = parent;
        Solver = solver;
        (Scope = scope).ForwardEventToLibraries(new SolverContextCreatedEvent(this));
    }

    public SolverContext CreateChild() => new(Solver, Scope, this);
    public static SolverContext Create(ErgoSolver solver, InterpreterScope scope) => new(solver, scope, null);
    public SolverContext GetRoot()
    {
        var root = this;
        while (root.Parent != null)
            root = root.Parent;
        return root;
    }

    /// <summary>
    /// Attempts to resolve 'goal' as a built-in call, and evaluates its result. On failure evaluates 'goal' as-is.
    /// </summary>
    public async IAsyncEnumerable<Evaluation> ResolveGoal(ITerm goal, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var builtins = scope.InterpreterScope.GetVisibleBuiltIns();

        var any = false;
        var sig = goal.GetSignature();
        if (!goal.IsQualified)
        {
            // Try resolving the built-in's module automatically
            foreach (var key in builtins.Keys)
            {
                if (!key.Module.TryGetValue(out var module) || !scope.InterpreterScope.IsModuleVisible(module))
                    continue;
                var withoutModule = key.WithModule(default);
                if (withoutModule.Equals(sig) || withoutModule.Equals(sig.WithArity(Maybe<int>.None)))
                {
                    goal = goal.Qualified(module);
                    sig = key;
                    break;
                }
            }
        }

        while (builtins.TryGetValue(sig, out var builtIn) || builtins.TryGetValue(sig = sig.WithArity(Maybe<int>.None), out builtIn))
        {
            if (ct.IsCancellationRequested)
                yield break;
            goal.GetQualification(out goal);
            var args = goal.GetArguments();
            Solver.LogTrace(SolverTraceType.BuiltInResolution, goal, scope.Depth);
            if (builtIn.Signature.Arity.TryGetValue(out var arity) && args.Length != arity)
            {
                scope.Throw(SolverError.UndefinedPredicate, sig.WithArity(args.Length).Explain());
                yield break;
            }

            await foreach (var eval in builtIn.Apply(this, scope, args.ToArray()))
            {
                if (ct.IsCancellationRequested)
                    yield break;
                goal = eval.Result;
                sig = goal.GetSignature();
                await foreach (var inner in ResolveGoal(eval.Result, scope, ct))
                {
                    yield return new(inner.Result, SubstitutionMap.MergeCopy(inner.Substitutions, eval.Substitutions));
                }

                any = true;
            }
        }

        if (!any)
            yield return new(goal);
    }

    public async IAsyncEnumerable<Solution> SolveAsync(Query goal, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        scope.InterpreterScope.ExceptionHandler.Throwing += Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught += Cancel;
        ct = CancellationTokenSource.CreateLinkedTokenSource(ct, ChoicePointCts.Token, ExceptionCts.Token).Token;
        await foreach (var s in SolveQuery(goal.Goals, scope, ct: ct))
        {
            yield return s;
        }
        scope.InterpreterScope.ExceptionHandler.Throwing -= Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught -= Cancel;
        void Cancel(ExceptionDispatchInfo _) => ExceptionCts.Cancel(false);
    }


    // This method takes a list of goals and solves them one at a time.
    // The tail of the list is fed back into this method recursively.
    private async IAsyncEnumerable<Solution> SolveQuery(NTuple query, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var tcoPred = Maybe<Predicate>.None;
        var tcoSubs = new SubstitutionMap();
    TCO:
        if (query.IsEmpty)
        {
            yield return new(scope, tcoSubs);
            yield break;
        }
        var goals = query.Contents;
        var subGoal = goals.First();
        goals = goals.RemoveAt(0);

        // Get first solution for the current subgoal
        await foreach (var s in SolveTerm(subGoal, scope, ct: ct))
        {
            if (ct.IsCancellationRequested) yield break;
            var rest = new NTuple(goals.Select(x => x.Substitute(s.Substitutions)));
            if (s.Scope.Callee.IsTailRecursive)
            {
                // SolveTerm returned early with a "fake" solution that signals SolveQuery to perform TCO on the callee.
                scope = s.Scope;
                tcoSubs.AddRange(s.Substitutions);
                query = new(s.Scope.Callee.Body.Contents.AddRange(rest.Contents));
                tcoPred = s.Scope.Callee;
                goto TCO;
            }
            if (rest.Contents.Length > 0 && tcoPred.TryGetValue(out var p))
            {
                var mostRecentCaller = s.Scope.Callers.Reverse().Prepend(s.Scope.Callee).FirstOrDefault(x => x.IsSameDefinitionAs(p));
                if (mostRecentCaller.Equals(p))
                {
                    scope = s.Scope;
                    query = new(rest.Contents);
                    goto TCO;
                }
                else
                    tcoPred = mostRecentCaller;
            }
            if (rest.Contents.Length == 0)
            {
                yield return s.AppendSubstitutions(tcoSubs);
                continue;
            }
            // Solve the rest of the goal
            await foreach (var ss in SolveQuery(rest, s.Scope, ct: ct))
            {
                if (ct.IsCancellationRequested) yield break;
                yield return ss.AppendSubstitutions(SubstitutionMap.MergeCopy(tcoSubs, s.Substitutions));
            }
        }
    }


    private async IAsyncEnumerable<Solution> SolveTerm(ITerm goal, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested) yield break;

        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException)
        {
            scope.Throw(SolverError.StackOverflow);
            yield break;
        }

    begin:
        if (goal.IsParenthesized)
            scope = scope.WithChoicePoint();

        // Treat comma-expression complex ITerms as proper expressions
        if (NTuple.FromPseudoCanonical(goal, default, default).TryGetValue(out var expr))
        {
            if (expr.Contents.Length == 1)
            {
                goal = expr.Contents.Single();
                goto begin; // gotos are used to prevent allocating unnecessary stack frames whenever possible
            }
            await foreach (var s in SolveQuery(expr, scope, ct: ct))
                yield return s;
            yield break;
        }

        // If goal resolves to a builtin, it is called on the spot and its solutions enumerated (usually just ⊤ or ⊥, plus a list of substitutions)
        // If goal does not resolve to a builtin it is returned as-is, and it is then matched against the knowledge base.
        await foreach (var resolvedGoal in ResolveGoal(goal, scope, ct: ct))
        {
            if (ct.IsCancellationRequested) yield break;
            if (resolvedGoal.Result.Equals(WellKnown.Literals.False) || resolvedGoal.Result is Variable)
            {
                Solver.LogTrace(SolverTraceType.BuiltInResolution, WellKnown.Literals.False, scope.Depth);
                yield break;
            }

            if (resolvedGoal.Result.Equals(WellKnown.Literals.True))
            {
                if (goal.Equals(WellKnown.Literals.Cut))
                    scope = scope.WithCut();

                yield return new(scope, resolvedGoal.Substitutions);
                if (scope.IsCutRequested)
                    ChoicePointCts.Cancel(false);
                continue;
            }

            // Attempts qualifying a goal with a module, then finds matches in the knowledge base
            var matches = ErgoSolver.GetImplicitGoalQualifications(resolvedGoal.Result, scope)
                .Select(x => Solver.KnowledgeBase.GetMatches(scope.InstantiationContext, x.Term, desugar: false))
                .FirstOrDefault(x => x.Any());
            if (matches is null)
            {
                var signature = resolvedGoal.Result.GetSignature();
                if (Solver.KnowledgeBase.Any(p => p.Head.GetSignature().Equals(signature)))
                    continue;
                var dyn = scope.InterpreterScope.Modules.Values
                    .SelectMany(m => m.DynamicPredicates)
                    .SelectMany(p => new[] { p, p.WithModule(default) })
                    .ToHashSet();
                if (dyn.Contains(signature))
                    continue;
                if (Solver.Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                    scope.Throw(SolverError.UndefinedPredicate, signature.Explain());
                ChoicePointCts.Cancel(false);
                yield break;
            }
            foreach (var m in matches)
            {
                // Create a new scope and context for this procedure call, essentially creating a choice point
                var innerScope = scope
                    .WithDepth(scope.Depth + 1)
                    .WithModule(m.Rhs.DeclaringModule)
                    .WithCallee(m.Rhs)
                    .WithCaller(scope.Callee)
                    .WithChoicePoint();
                if (m.Rhs.IsTailRecursive)
                {
                    // Yield a "fake" solution to the caller, which will then use it to perform TCO
                    yield return new(innerScope, SubstitutionMap.MergeCopy(m.Substitutions, resolvedGoal.Substitutions));
                    continue;
                }
                using var innerCtx = CreateChild();
                Solver.LogTrace(SolverTraceType.Call, m.Lhs, scope.Depth);
                await foreach (var s in innerCtx.SolveAsync(new(m.Rhs.Body), innerScope, ct: ct))
                {
                    Solver.LogTrace(SolverTraceType.Exit, m.Rhs.Head, s.Scope.Depth);
                    yield return s.PrependSubstitutions(SubstitutionMap.MergeCopy(m.Substitutions, resolvedGoal.Substitutions));
                }
                if (innerCtx.ChoicePointCts.IsCancellationRequested)
                    break;
                if (innerCtx.ExceptionCts.IsCancellationRequested)
                    ExceptionCts.Cancel(false);

            }
        }
    }

    public void Dispose()
    {
        Scope.ForwardEventToLibraries(new SolverContextDisposedEvent(this));
    }
}
