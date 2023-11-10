using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using System.Diagnostics;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    #region Type Declarations
    public enum ErrorType
    {
        MatchFailed
    }
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
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
                        var @continue = vm.@continue;
                        vm.@continue = vm => ContinueFrom(vm, k);
                        goals[i](vm);
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
            condition(vm);
            vm.Cut();
            if (vm.State != VMState.Fail)
            {
                if (vm.State == VMState.Solution)
                    vm.MergeEnvironment();
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
        /// <summary>
        /// Performs the unification at the time when Unify is called.
        /// Either returns Ops.Fail or Ops.UpdateEnvironment with the result of unification.
        /// See also UnifyLazy for a version that delays unification until necessary.
        /// </summary>
        public static Op Unify(ITerm left, ITerm right)
        {
            // In this case unification is really just the act of updating the environment with the *result* of unification.
            // The Op is provided for convenience and as a wrapper. Note that unification is performed eagerly in this case. 
            if (Substitution.Unify(new(left, right)).TryGetValue(out var subs))
            {
                return UpdateEnvironment(subs);
            }
            return Fail;
        }
        /// <summary>
        /// This should not be required by anything in standard Ergo, but some extensions may benefit from
        /// delaying term unification until the very moment it should be performed. Standard terms are 
        /// immutable, so they wouldn't change between the call to UnifyLazy and the moment when the op is
        /// executed on the VM. But if a term can change e.g. on backtracking, then UnifyLazy is the way to go.
        /// </summary>
        public static Op UnifyLazy(ITerm left, ITerm right)
        {
            // The difference from just returning Unify(left, right) is that, this way,
            // the args are captured by the closure and evaluated *every time the op is ran*,
            // including on backtracking. Stateful abstract terms may unify to something else by then.
            return vm => Unify(left, right)(vm);
        }
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
        /// Calls a built-in by passing it the matching goal's arguments.
        /// </summary>
        public static Op BuiltInGoal(ITerm goal, SolverBuiltIn builtIn) => vm =>
        {
            goal.Substitute(vm.Environment).GetQualification(out var inst);
            var args = inst.GetArguments();
            var op = builtIn.Compile(args);
            vm.LogState($"B:" + goal.Explain(false));
            // Temporary: once Solver is dismantled, remove this check and allow a builtin to resolve to noop.
            if (NoOp != op)
            {
                op(vm);
                return;
            }
            #region temporary code
            var expl = goal.Explain(false);
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
                // TODO: check if substituting does anything; if not, move outer part of method to Goal
                var inst = goal.Substitute(vm.Environment);
                if (!vm.KnowledgeBase.GetMatches(vm.InstCtx, inst, false).TryGetValue(out var matches))
                {
                    // Static and dynamic predicates should have been resolved by now, so a failing match is an error.
                    Throw(ErrorType.MatchFailed, inst.Explain(false));
                    return;
                }
                var matchEnum = matches.GetEnumerator();
                NextMatch(vm);
                void NextMatch(ErgoVM vm)
                {
                    if (matchEnum.MoveNext())
                    {
                        vm.PushChoice(NextMatch);
                        vm.Environment.AddRange(matchEnum.Current.Substitutions);
                        var pred = matchEnum.Current.Predicate;
                        vm.LogState($"M:" + pred.Body.Explain(false));
                        if (pred.BuiltIn.TryGetValue(out var builtIn))
                            BuiltInGoal(pred.Head, builtIn)(vm);
                        else if (pred.ExecutionGraph.TryGetValue(out var graph))
                            graph.Compile()(vm);
                        else if (!pred.IsFactual) // probably a dynamic goal with no associated graph
                            Goals(pred.Body)(vm);
                        vm.SuccessToSolution();
                        Substitution.Pool.Release(matchEnum.Current.Substitutions);
                    }
                    else
                    {
                        vm.Fail();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a continuation point for the VM to backtrack to and a snapshot of the VM at the time when this choice point was created.
    /// </summary>
    public readonly record struct ChoicePoint(Op Continue, SubstitutionMap Environment);
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
    protected Op @continue;
    #endregion
    #region VM API
    /// <summary>
    /// Represents the current execution state of the VM.
    /// </summary>
    public VMState State { get; private set; } = VMState.Ready;
    /// <summary>
    /// The active set of substitutions.
    /// </summary>
    public SubstitutionMap Environment { get; private set; }
    /// <summary>
    /// The current set of solutions. See also RunInteractive.
    /// </summary>
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(Scope, x));
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
    protected void MergeEnvironment()
    {
        var subs = solutions.Pop();
        Environment.AddRange(subs);
        Substitution.Pool.Release(subs);
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
