using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using System.Diagnostics;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    #region Type Declarations
    public enum ErrorType
    {
        MatchFailed,
        StackEmpty
    }
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
    public delegate Op Goal(ImmutableArray<ITerm> args);
    public static class Goals
    {
        public static Goal True => _ => Ops.NoOp;
        public static Goal False => _ => Ops.Fail;
        /// <summary>
        /// Performs the unification at the time when Unify is called.
        /// Either returns Ops.Fail or Ops.UpdateEnvironment with the result of unification.
        /// </summary>
        public static Goal Unify => args =>
        {
            // In this case unification is really just the act of updating the environment with the *result* of unification.
            // The Op is provided for convenience and as a wrapper. Note that unification is performed eagerly in this case. 
            if (args[0].Unify(args[1]).TryGetValue(out var subs))
                return Ops.UpdateEnvironment(subs);
            return Ops.Fail;
        };
        /// <summary>
        /// Calls a built-in by passing it the matching goal's arguments.
        /// </summary>
        public static Goal BuiltIn(SolverBuiltIn builtIn) => args => vm =>
        {
            // goal.Substitute(vm.Environment).GetQualification(out var inst);
            // var args = inst.GetArguments();
            var op = builtIn.Compile()(args);
            // Temporary: once Solver is dismantled, remove this check and allow a builtin to resolve to noop.
            if (Ops.NoOp != op)
            {
                op(vm);
                return;
            }
            #region temporary code
            var next = builtIn.Apply(vm.Context, vm.Scope, args).GetEnumerator();
            NextGoal(vm);
            void NextGoal(ErgoVM vm)
            {
                if (next.MoveNext())
                {
                    if (!next.Current.Result)
                    {
                        vm.Fail();
                        return;
                    }
                    vm.Solution(next.Current.Substitutions);
                    vm.PushChoice(NextGoal);
                }
                else
                {
                    vm.Fail();
                }
            }
            #endregion
        };
    }
    public static class Ops
    {
        public static Op NoOp => _ => { };
        public static Op Fail => vm => vm.Fail();
        public static Op Cut => vm => vm.Cut();
        public static Op Solution => vm => vm.Solution();
        public static Op And(params Op[] goals)
        {
            if (goals.Length == 0)
                return NoOp;
            if (goals.Length == 1)
                return goals[0];
            return vm =>
            {
                ContinueFrom(vm, 0);
                void ContinueFrom(ErgoVM vm, int j)
                {
                    if (j >= goals.Length) return;
                    for (int i = j; i < goals.Length; i++)
                    {
                        var k = i + 1;
                        // Cache continuation so that goals calling PushChoice know where to continue from.
                        var @continue = vm.@continue;
                        vm.@continue = @continue == NoOp
                            ? vm => ContinueFrom(vm, k)
                            : vm => { @continue(vm); ContinueFrom(vm, k); };
                        goals[i](vm);
                        // Restore previous continuation before potentially yielding control to another And.
                        vm.@continue = @continue;
                        switch (vm.State)
                        {
                            case VMState.Fail: return;
                            case VMState.Solution: vm.MergeEnvironment(); break;
                        }
                    }
                    vm.Solution();
                }
            };
        }
        public static Op Or(params Op[] branches)
        {
            if (branches.Length == 0)
                return NoOp;
            if (branches.Length == 1)
                return branches[0];
            return vm =>
            {
                for (int i = branches.Length - 1; i >= 1; i--)
                {
                    vm.PushChoice(Branch(branches[i]));
                }
                Branch(branches[0])(vm);

                Op Branch(Op branch) => vm =>
                {
                    branch(vm);
                    vm.SuccessToSolution();
                };
            };
        }
        public static Op IfThenElse(Op condition, Op consequence, Op alternative) => vm =>
        {
            var backupEnvironment = vm.CloneEnvironment();
            var numCp = vm.NumChoicePoints;
            condition(vm);
            if (vm.State != VMState.Fail)
            {
                if (vm.State == VMState.Solution)
                    vm.MergeEnvironment();
                // Discard choice points created by the condition. Similar to a cut but more specific.
                vm.DiscardChoices(vm.NumChoicePoints - numCp);
                consequence(vm);
            }
            else
            {
                vm.State = VMState.Success;
                vm.Environment = backupEnvironment;
                alternative(vm);
                vm.SuccessToSolution();
            }
        };
        public static Op IfThen(Op condition, Op consequence) => IfThenElse(condition, consequence, NoOp);
        /// <summary>
        /// Adds the current set of substitutions to te VM's environment, and then releases it back into the substitution map pool.
        /// </summary>
        public static Op UpdateEnvironment(SubstitutionMap subsToAdd) => vm =>
        {
            vm.Environment.AddRange(subsToAdd);
            Substitution.Pool.Release(subsToAdd);
        };
        public static Op SetEnvironment(SubstitutionMap newEnv) => vm =>
        {
            Substitution.Pool.Release(vm.Environment);
            vm.Environment = newEnv;
        };
        /// <summary>
        /// Converts a query into the corresponding Op.
        /// </summary>
        public static Op Goals(NTuple goals)
        {
            if (goals.Contents.Length == 0)
                return NoOp;
            if (goals.Contents.Length == 1)
                return Goal(goals.Contents[0]);
            return And(goals.Contents.Select(Goal).ToArray());
        }
        /// <summary>
        /// Calls an individual goal.
        /// </summary>
        public static Op Goal(ITerm goal)
        {
            const string cutValue = "!";
            return goal switch
            {
                NTuple tup => Goals(tup),
                Atom { Value: true } => NoOp,
                Atom { Value: false } => Fail,
                Atom { Value: cutValue, IsQuoted: false } => Cut,
                _ => Resolve
            };

            void Resolve(ErgoVM vm)
            {
                goal = goal.Substitute(vm.Environment);
                var matchEnum = GetEnumerator(vm);
                NextMatch(vm);
                void NextMatch(ErgoVM vm)
                {
                    var anyMatch = false;
                TCO:
                    // In the non-tail recursive case, you can imagine this 'while' as if it were an 'if'.
                    while (matchEnum.MoveNext())
                    {
                        anyMatch = true;
                        // Push a choice point for this match. If it fails, it will be retried until there are no more matches.
                        vm.PushChoice(NextMatch);
                        // Update the environment by adding the current match's substitutions.
                        vm.Environment.AddRange(matchEnum.Current.Substitutions);
                        // Decide how to execute this goal depending on whether:
                        Op runGoal = NoOp;
                        var pred = matchEnum.Current.Predicate;
                        // - It's a builtin (we can run it directly with low overhead)
                        if (pred.BuiltIn.TryGetValue(out var builtIn))
                        {
                            pred.Head.Substitute(vm.Environment).GetQualification(out var inst);
                            runGoal = ErgoVM.Goals.BuiltIn(builtIn)(inst.GetArguments());
                        }
                        // - It has an execution graph (we can run it directly with low overhead if there's a cached compiled version)
                        else if (pred.ExecutionGraph.TryGetValue(out var graph))
                            runGoal = graph.Compile();
                        // - It has to be interpreted (we have to run it traditionally)
                        else if (!pred.IsFactual) // probably a dynamic goal with no associated graph
                            runGoal = Goals(pred.Body);
                        // Track the number of choice points before executing the goal (up to the one we just pushed)
                        var numCp = vm.NumChoicePoints;
                        // Actually execute the goal. This may produce success, a solution, or set the VM in a failure state.
                        runGoal(vm);
                        // If the VM is in success state, promote that success to a solution by pushing the current environment.
                        vm.SuccessToSolution();
                        // If this is a tail call of pred, then we can recycle the current stack frame (hence the top-level 'while').
                        if (pred.IsTailRecursive && Predicate.IsTailCall(goal, pred.Body))
                        {
                            // -- Assumes that pred is det; TODO: static analysis
                            // Pop all choice points that were created by this predicate.
                            // TODO: figure out if this is actually the correct thing to do.
                            while (vm.NumChoicePoints > numCp)
                            {
                                var cp = vm.PopChoice().GetOr(default);
                                // Set the environment to that of the oldest popped choice point.
                                Substitution.Pool.Release(vm.Environment);
                                vm.Environment = cp.Environment;
                                // If runGoal failed, set the vm back to success as we're retrying now.
                                if (vm.State == VMState.Fail)
                                    vm.State = VMState.Success;
                            }
                            // If the above loop didn't run and runGoal failed, then we can't retry so we exit the outer loop.
                            if (vm.State == VMState.Fail)
                                break;
                            // Keep the list of substitutions that contributed to this iteration.
                            var bodyVars = pred.Body.Variables.ToHashSet();
                            var tcoSubs = vm.Environment
                                .Where(s => bodyVars.Contains((Variable)s.Lhs));
                            // Substitute the tail call with this list, creating the new head, and qualify it with the current module.
                            goal = pred.Body.Contents.Last().Substitute(tcoSubs)
                                .Qualified(pred.DeclaringModule);
                            // Remove all substitutions that are no longer relevant, including those we just used.
                            vm.Environment.RemoveRange(tcoSubs.Concat(matchEnum.Current.Substitutions));
                            vm.DiscardChoices(1); // We don't need the NextMatch choice point anymore.
                            matchEnum = GetEnumerator(vm);
                            goto TCO;
                        }
                        // Non-tail recursive predicates don't benefit from the while loop and must backtrack as normal.
                        else break;
                    }
                    // If the 'while' above were an 'if', this would be the 'else' branch.
                    if (!anyMatch)
                    {
                        // Essentially, when we exhaust the list of matches for 'goal', we set the VM in a failure state to signal backtracking.
                        vm.Fail();
                    }
                }
            }
            IEnumerator<KBMatch> GetEnumerator(ErgoVM vm)
            {
                if (!vm.KnowledgeBase.GetMatches(vm.InstCtx, goal, false).TryGetValue(out var matches))
                {
                    // Static and dynamic predicates should have been resolved by now, so a failing match is an error.
                    Throw(ErrorType.MatchFailed, goal.Explain(false));
                    return Enumerable.Empty<KBMatch>().GetEnumerator();
                }
                return matches.GetEnumerator();
            }
        }
    }

    /// <summary>
    /// Represents a continuation point for the VM to backtrack to and a snapshot of the VM at the time when this choice point was created.
    /// </summary>
    public readonly record struct ChoicePoint(Op Continue, SubstitutionMap Environment);
    public readonly record struct TailCallData(ExecutionNode Graph, Predicate Clause);
    public enum VMState { Ready, Fail, Solution, Success }
    #endregion
    // Temporary, these two properties will be removed in due time.
    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }
    public KnowledgeBase KnowledgeBase { get; set; }
    public readonly InstantiationContext InstCtx = new("VM");
    #region Internal VM State
    protected Stack<ChoicePoint> choicePoints = new();
    protected Stack<SubstitutionMap> solutions = new();
    protected int cutIndex;
    public Op @continue;
    #endregion
    #region VM API
    /// <summary>
    /// Represents the current execution state of the VM.
    /// </summary>
    public VMState State { get; private set; } = VMState.Ready;
    /// <summary>
    /// The active set of substitutions.
    /// </summary>
    public SubstitutionMap Environment { get; set; }
    /// <summary>
    /// The current set of solutions. See also RunInteractive.
    /// </summary>
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(Scope, x));
    public int NumChoicePoints => choicePoints.Count;
    private Op _query = Ops.NoOp;
    public Op Query
    {
        get => _query;
        set => _query = value ?? Ops.NoOp;
    }
    public void Run()
    {
        Initialize();
        State = VMState.Success;
        Query(this);
        Backtrack();
        CleanUp();
    }
    /// <summary>
    /// Starting enumeration will cause the VM to be run in interactive mode, yielding one solution at a time. See also Solutions.
    /// </summary>
    public IEnumerable<Solution> RunInteractive()
    {
        Initialize();
        State = VMState.Success;
        Query(this);
        while (State != VMState.Ready)
        {
            while (solutions.TryPop(out var sol))
                yield return new Solution(Scope, sol);
            if (!BacktrackOnce())
                break;
        }
        CleanUp();
        while (solutions.TryPop(out var sol))
            yield return new Solution(Scope, sol);
    }
    #endregion
    #region Goal API
    public static void Throw(ErrorType error, params object[] args)
    {
        throw new RuntimeException(error, args);
    }

    public void Fail()
    {
        State = VMState.Fail;
    }
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        solutions.Push(subs);
        State = VMState.Solution;
        LogState();
    }
    public void Solution()
    {
        solutions.Push(CloneEnvironment());
        State = VMState.Solution;
        LogState();
    }
    public void Cut()
    {
        cutIndex = choicePoints.Count;
    }
    public Maybe<ChoicePoint> PopChoice()
    {
        if (choicePoints.TryPop(out var ret))
            return ret;
        return default;
    }

    private static readonly Exception StackEmptyException = new RuntimeException(ErrorType.StackEmpty);
    public void DiscardChoices(int numChoices)
    {
        while (numChoices-- > 0)
        {
            var cp = PopChoice().GetOrThrow(StackEmptyException);
            Substitution.Pool.Release(cp.Environment);
        }
    }

    public void PushChoice(Op choice)
    {
        var env = CloneEnvironment();
        var cont = @continue;
        if (cont == Ops.NoOp)
            choicePoints.Push(new ChoicePoint(choice, env));
        else
            choicePoints.Push(new ChoicePoint(vm => Ops.And(choice, cont)(vm), env));
    }
    #endregion

    [Conditional("ERGO_VM_DIAGNOSTICS")]
    protected void LogState([CallerMemberName] string caller = null)
    {
        Trace.WriteLine($"{State} {{{Environment.Select(x => x.Explain()).Join(", ")}}} ({@continue.Method.Name}) @ {caller}");
    }
    private void SuccessToSolution()
    {
        if (State == VMState.Success)
            Solution();
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop();
        Ops.UpdateEnvironment(subs)(this);
        State = VMState.Success;
    }
    protected SubstitutionMap CloneEnvironment()
    {
        var env = Substitution.Pool.Acquire();
        env.AddRange(Environment);
        return env;
    }
    protected void Backtrack()
    {
        while (BacktrackOnce()) ;
    }
    protected virtual bool BacktrackOnce()
    {
        if (cutIndex < choicePoints.Count)
        {
            State = VMState.Success;
            var choicePoint = choicePoints.Pop();
            Substitution.Pool.Release(Environment);
            Environment = choicePoint.Environment;
            choicePoint.Continue(this);
            SuccessToSolution();
            LogState();
            return true;
        }
        return false;
    }
    protected virtual void Initialize()
    {
        State = VMState.Ready;
        Environment = new();
        cutIndex = 0;
        @continue = Ops.NoOp;
    }
    protected virtual void CleanUp()
    {
        SuccessToSolution();
        Substitution.Pool.Release(Environment);
    }
}
