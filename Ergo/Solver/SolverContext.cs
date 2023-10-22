using Ergo.Debugger;
using Ergo.Events.Solver;
using Ergo.Interpreter;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Ergo.Solver;

public class ErgoTask
{
    public readonly ITerm Query;
}

// TODO: Build a stack-based system that supports last call optimization
public sealed class SolverContext : IDisposable
{
    public readonly SolverContext Parent;

    private DebuggerState _debugState = DebuggerState.Running;
    private readonly ManualResetEvent _debuggerSignal = new(true);
    private readonly CancellationTokenSource _choicePointCts;
    private readonly CancellationTokenSource _exceptionCts;
    private readonly ConcurrentBag<ErgoTask> _tasks;

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

    public void Cut()
    {
        _choicePointCts.Cancel(false);
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

        // Get each solution for the current subgoal
        foreach (var s in SolveTerm(subGoal, scope, ct: ct))
        {
            var rest = new NTuple(goals.Select(x => x.Substitute(s.Substitutions)), query.Scope);
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
                query = new(s.Scope.Callee.Body.Contents.Concat(rest.Contents), query.Scope);
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

        if (goal is Atom { Value: true })
        {
            yield return new(scope, new());
            yield break;
        }

        if (goal is Atom { Value: false })
        {
            yield break;
        }

        // Treat comma-expression complex ITerms as proper expressions
        if (goal is NTuple goalList)
        {
            if (goalList.Contents.Length == 1)
            {
                goal = goalList.Contents.Single();
                goto begin; // gotos are used to prevent allocating unnecessary stack frames whenever possible
            }
            foreach (var s in SolveQuery(goalList, scope, ct: ct))
            {
                _debuggerSignal.WaitOne();
                yield return s;
            }
            yield break;
        }
        // Attempts qualifying a goal with a module, then finds matches in the knowledge base
        var noMatches = true;
        var matches = ErgoSolver.GetImplicitGoalQualifications(goal, scope)
            .Select(x => Solver.KnowledgeBase.GetMatches(scope.InstantiationContext, x, desugar: false))
            .Where(x => x.TryGetValue(out _) && !(noMatches = false))
            .Take(1)
            .SelectMany(m => m.AsEnumerable().SelectMany(x => x));
        foreach (var m in matches)
        {
            // Create a new scope and context for this procedure call, essentially creating a choice point
            var innerScope = scope
                .WithDepth(scope.Depth + 1)
                .WithModule(m.Predicate.DeclaringModule)
                .WithCallee(m.Predicate)
                .WithCaller(scope.Callee)
                .WithChoicePoint();
            scope.Trace(SolverTraceType.Call, m.Goal);
            if (m.Predicate.BuiltIn.TryGetValue(out var builtIn))
            {
                foreach (var eval in builtIn.Apply(this, scope, goal.GetArguments()))
                {
                    if (!eval.Result)
                        yield break;
                    else
                        yield return new(scope, eval.Substitutions);
                }
                continue;
            }
#if !ERGO_SOLVER_DISABLE_TCO
            if (m.Predicate.IsTailRecursive)
            {
                // PROBLEM: This branch should only be entered iff the predicate is known to be determinate at this point
                // https://sicstus.sics.se/sicstus/docs/3.12.8/html/sicstus/Last-Clause-Determinacy-Detection.html
                // https://sicstus.sics.se/sicstus/docs/3.12.8/html/sicstus/What-is-Detected.html#What-is-Detected
                // https://www.mercurylang.org/information/doc-latest/mercury_ref/Determinism.html#Determinism-categories
                // https://www.metalevel.at/prolog/fun
                // Yield a "fake" solution to the caller, which will then use it to perform TCO
                scope.Trace(SolverTraceType.TailCallOptimization, m.Goal);
                _debuggerSignal.WaitOne();
                yield return new(innerScope, m.Substitutions);
                continue;
            }
#endif
            using var innerCtx = CreateChild();
            foreach (var s in innerCtx.SolveQuery(m.Predicate.Body, innerScope, ct: ct))
            {
                scope.Trace(SolverTraceType.Exit, m.Predicate.Head.Substitute(s.Substitutions));
                yield return s.PrependSubstitutions(m.Substitutions);
            }
            if (innerCtx._choicePointCts.IsCancellationRequested)
                break;
            // Backtrack
            scope.Trace(SolverTraceType.Backtrack, m.Goal);
            _debuggerSignal.WaitOne();
        }
        if (noMatches)
        {
            var signature = goal.GetSignature();
            if (Solver.KnowledgeBase.Any(p => p.Head.GetSignature().Equals(signature)))
                yield break;
            var dyn = scope.InterpreterScope.Modules.Values
                .SelectMany(m => m.DynamicPredicates)
                .SelectMany(p => new[] { p, p.WithModule(default) })
                .ToHashSet();
            if (dyn.Contains(signature))
                yield break;
            if (Solver.Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                scope.Throw(SolverError.UndefinedPredicate, signature.Explain());
            _choicePointCts.Cancel(false);
            yield break;
        }
    }

    public void Dispose()
    {
        Scope.ExceptionHandler.Throwing -= ExceptionHandler_Throwing;
        Scope.ForwardEventToLibraries(new SolverContextDisposedEvent(this));
    }
}
