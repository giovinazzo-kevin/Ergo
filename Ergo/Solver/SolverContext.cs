using Ergo.Debugger;
using Ergo.Events.Solver;
using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;
using System.Runtime.ExceptionServices;

namespace Ergo.Solver;

// TODO: Build a stack-based system that supports last call optimization
public sealed class SolverContext : IDisposable
{
    public readonly SolverContext Parent;

    private DebuggerState _debugState = DebuggerState.Running;
    private readonly ManualResetEvent _debuggerSignal = new(true);
    private readonly CancellationTokenSource _choicePointCts;
    private readonly CancellationTokenSource _exceptionCts;

    public readonly InterpreterScope Scope;
    public readonly ErgoSolver Solver;

    private SolverContext(ErgoSolver solver, InterpreterScope scope, SolverContext parent, CancellationTokenSource choicePointCts, CancellationTokenSource exceptionCts)
    {
        Parent = parent;
        Solver = solver;
        (Scope = scope).ForwardEventToLibraries(new SolverContextCreatedEvent(this));
        _choicePointCts = choicePointCts;
        _exceptionCts = exceptionCts;
        Scope.ExceptionHandler.Throwing += ExceptionHandler_Throwing;
    }

    private void ExceptionHandler_Throwing(ExceptionDispatchInfo obj)
    {
        _debuggerSignal.WaitOne();
    }

    public void PauseExecution()
    {
        if (_debugState == DebuggerState.Running)
        {
            _debuggerSignal.Reset();
            _debugState = DebuggerState.Paused;
        }
    }

    public void ResumeExecution()
    {
        if (_debugState == DebuggerState.Paused)
        {
            _debuggerSignal.Set();
            _debugState = DebuggerState.Running;
        }
    }
    public SolverContext CreateChild() => new(Solver, Scope, this, new(), _exceptionCts);
    public static SolverContext Create(ErgoSolver solver, InterpreterScope scope) => new(solver, scope, null, new(), new());
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
    public IEnumerable<Evaluation> ResolveGoal(ITerm goal, SolverScope scope, CancellationToken ct = default)
    {
        var any = false;
        var sig = goal.GetSignature();
        if (!goal.IsQualified)
        {
            // Try resolving the built-in's module automatically
            foreach (var key in scope.InterpreterScope.VisibleBuiltInsKeys)
            {
                if (!key.Module.TryGetValue(out var module))
                    continue;
                var withoutModule = key.WithModule(default);
                if (withoutModule.Equals(sig))
                {
                    goal = goal.Qualified(module);
                    sig = key;
                    break;
                }
            }
        }

        if (scope.InterpreterScope.VisibleBuiltIns.TryGetValue(sig, out var builtIn))
        {
            goal.GetQualification(out goal);
            var args = goal.GetArguments();
            scope.Trace(SolverTraceType.BuiltInResolution, goal);
            if (builtIn.Signature.Arity.TryGetValue(out var arity) && args.Length != arity)
            {
                scope.Throw(SolverError.UndefinedPredicate, sig.WithArity(args.Length).Explain());
                yield break;
            }

            foreach (var eval in builtIn.Apply(this, scope, args.ToArray()))
            {
                _debuggerSignal.WaitOne();
                goal = eval.Result;
                sig = goal.GetSignature();
                yield return eval;
                any = true;
            }
        }

        if (!any)
            yield return new(goal);
    }

    public IEnumerable<Solution> Solve(Query goal, SolverScope scope, CancellationToken ct = default)
    {
        scope.InterpreterScope.ExceptionHandler.Throwing += Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught += Cancel;
        ct = CancellationTokenSource.CreateLinkedTokenSource(ct, _exceptionCts.Token).Token;
        if (!ct.IsCancellationRequested)
        {
            foreach (var s in SolveQuery(goal.Goals, scope, ct: ct))
            {
                yield return s;
                if (ct.IsCancellationRequested)
                    yield break;
            }
        }
        scope.InterpreterScope.ExceptionHandler.Throwing -= Cancel;
        scope.InterpreterScope.ExceptionHandler.Caught -= Cancel;
        void Cancel(ExceptionDispatchInfo _) => _exceptionCts.Cancel(false);
    }


    // This method takes a list of goals and solves them one at a time.
    // The tail of the list is fed back into this method recursively.
    private IEnumerable<Solution> SolveQuery(NTuple query, SolverScope scope, CancellationToken ct = default)
    {
        var tcoPred = Maybe<Predicate>.None;
        var tcoVars = new HashSet<Variable>();
        var tcoSubs = new SubstitutionMap();
    TCO:
        if (ct.IsCancellationRequested)
            yield break;
        if (query.IsEmpty)
        {
            yield return new(scope, tcoSubs);
            yield break;
        }
        var goals = query.Contents;
        var subGoal = goals.First();
        goals = goals.RemoveAt(0);

        // Get first solution for the current subgoal
        foreach (var s in SolveTerm(subGoal, scope, ct: ct))
        {
            var rest = new NTuple(goals.Select(x => x.Substitute(s.Substitutions)));
#if !ERGO_SOLVER_DISABLE_TCO
            if (s.Scope.Callee.IsTailRecursive && Predicate.IsLastCall(subGoal, s.Scope.Callee.Body))
            {
                // SolveTerm returned early with a "fake" solution that signals SolveQuery to perform TCO on the callee.
                if (s.Scope.Callers.Any())
                    scope = s.Scope.WithoutLastCaller().WithDepth(s.Scope.Depth - 1);
                tcoSubs.AddRange(s.Substitutions);
                tcoVars.UnionWith(s.Variables);
                // Remove all substitutions that don't pertain to any variables in the current scope
                tcoSubs.Prune(tcoVars);
                query = new(s.Scope.Callee.Body.Contents.Concat(rest.Contents));
                tcoPred = s.Scope.Callee;
                goto TCO;
            }
            if (rest.Contents.Length > 0 && tcoPred.TryGetValue(out var p))
            {
                var mostRecentCaller = s.Scope.Callers.Reverse().Prepend(s.Scope.Callee).FirstOrDefault(x => x.IsSameDeclarationAs(p));
                if (mostRecentCaller.Equals(p))
                {
                    tcoSubs.AddRange(s.Substitutions);
                    tcoVars.UnionWith(s.Variables);
                    tcoSubs.Prune(tcoVars);
                    query = rest;
                    goto TCO;
                }
                else
                    tcoPred = mostRecentCaller;
            }
#endif
            if (rest.Contents.Length == 0)
            {
                yield return s.PrependSubstitutions(tcoSubs);
                continue;
            }
            if (ct.IsCancellationRequested || _choicePointCts.IsCancellationRequested)
                yield break; // break on cuts and exceptions
            // Solve the rest of the goal
            foreach (var ss in SolveQuery(rest, s.Scope, ct: ct))
            {
                var lastSubs = SubstitutionMap.MergeCopy(tcoSubs, s.Substitutions);
                yield return ss.AppendSubstitutions(lastSubs);
            }
        }
    }


    private IEnumerable<Solution> SolveTerm(ITerm goal, SolverScope scope, CancellationToken ct = default)
    {
        _debuggerSignal.WaitOne();
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
            foreach (var s in SolveQuery(expr, scope, ct: ct))
            {
                _debuggerSignal.WaitOne();
                yield return s;
            }
            yield break;
        }

        // If goal resolves to a builtin, it is called on the spot and its solutions enumerated (usually just ⊤ or ⊥, plus a list of substitutions)
        // If goal does not resolve to a builtin it is returned as-is, and it is then matched against the knowledge base.
        foreach (var resolvedGoal in ResolveGoal(goal, scope, ct: ct))
        {
            _debuggerSignal.WaitOne();
            if (resolvedGoal.Result.Equals(WellKnown.Literals.False) || resolvedGoal.Result is Variable)
            {
                scope.Trace(SolverTraceType.BuiltInResolution, WellKnown.Literals.False);
                yield break;
            }

            if (resolvedGoal.Result.Equals(WellKnown.Literals.True))
            {
                yield return new(scope, resolvedGoal.Substitutions);
                if (goal.Equals(WellKnown.Literals.Cut))
                    _choicePointCts.Cancel(false);
                continue;
            }

            // Attempts qualifying a goal with a module, then finds matches in the knowledge base
            var noMatches = true;
            var matches = ErgoSolver.GetImplicitGoalQualifications(resolvedGoal.Result, scope)
                .Select(x => Solver.KnowledgeBase.GetMatches(scope.InstantiationContext, x, desugar: false))
                .Where(x => x.TryGetValue(out _) && !(noMatches = false))
                .Take(1)
                .SelectMany(m => m.AsEnumerable().SelectMany(x => x));
            foreach (var m in matches)
            {
                // Create a new scope and context for this procedure call, essentially creating a choice point
                var innerScope = scope
                    .WithDepth(scope.Depth + 1)
                    .WithModule(m.Rhs.DeclaringModule)
                    .WithCallee(m.Rhs)
                    .WithCaller(scope.Callee)
                    .WithChoicePoint();
                var callerVars = scope.Callee.Body.CanonicalForm.Variables;
                var calleeVars = m.Rhs.Body.CanonicalForm.Variables;
#if !ERGO_SOLVER_DISABLE_TCO
                if (m.Rhs.IsTailRecursive)
                {
                    // PROBLEM: This branch should only be entered iff the predicate is known to be determinate at this point
                    // https://sicstus.sics.se/sicstus/docs/3.12.8/html/sicstus/Last-Clause-Determinacy-Detection.html
                    // https://sicstus.sics.se/sicstus/docs/3.12.8/html/sicstus/What-is-Detected.html#What-is-Detected
                    // https://www.mercurylang.org/information/doc-latest/mercury_ref/Determinism.html#Determinism-categories
                    // https://www.metalevel.at/prolog/fun
                    // Yield a "fake" solution to the caller, which will then use it to perform TCO
                    scope.Trace(SolverTraceType.TailCallOptimization, m.Lhs);
                    _debuggerSignal.WaitOne();
                    yield return new(innerScope, SubstitutionMap.MergeRef(m.Substitutions, resolvedGoal.Substitutions));
                    continue;
                }
#endif
                using var innerCtx = CreateChild();
                scope.Trace(SolverTraceType.Call, m.Lhs);
                foreach (var s in innerCtx.SolveQuery(m.Rhs.Body, innerScope, ct: ct))
                {
                    scope.Trace(SolverTraceType.Exit, m.Rhs.Head.Substitute(s.Substitutions));
                    var innerSubs = SubstitutionMap.MergeRef(m.Substitutions, resolvedGoal.Substitutions);
                    yield return s.PrependSubstitutions(innerSubs);
                }
                if (innerCtx._choicePointCts.IsCancellationRequested)
                    break;
                // Backtrack
                scope.Trace(SolverTraceType.Backtrack, m.Lhs);
                _debuggerSignal.WaitOne();
            }
            if (noMatches)
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
                _choicePointCts.Cancel(false);
                yield break;
            }
        }
    }

    public void Dispose()
    {
        Scope.ExceptionHandler.Throwing -= ExceptionHandler_Throwing;
        Scope.ForwardEventToLibraries(new SolverContextDisposedEvent(this));
    }
}
