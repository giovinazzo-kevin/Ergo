using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
    /// <summary>
    /// Represents a continuation point for the VM to backtrack to and a snapshot of the VM at the time when this choice point was created.
    /// </summary>
    public readonly record struct ChoicePoint(Op Continue, SubstitutionMap Environment);
    public enum VMState { Ready, Fail, Solution, Success }

    // Temporary, these two properties will be removed in due time.
    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }
    public KnowledgeBase KnowledgeBase { get; set; }

    protected Stack<ChoicePoint> choicePoints = new();
    protected Stack<SubstitutionMap> solutions = new();
    protected int cutIndex = int.MaxValue;
    protected Queue<Op> rest;
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

    private Op _query = NoOp;
    public Op Query
    {
        get => _query;
        set => _query = value ?? NoOp;
    }
    #region Internal VM State
    public void Fail()
    {
        State = VMState.Fail;
    }
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        solutions.Push(subs);
        State = VMState.Solution;
    }
    public void Solution()
    {
        solutions.Push(CloneEnvironment());
        State = VMState.Solution;
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop();
        Environment.AddRange(subs);
        Substitution.Pool.Release(subs);
        State = VMState.Success;
    }
    public SubstitutionMap CloneEnvironment()
    {
        var env = Substitution.Pool.Acquire();
        env.AddRange(Environment);
        return env;
    }
    public void Cut()
    {
        cutIndex = choicePoints.Count;
    }
    public void PushChoice(Op choice)
    {
        var env = CloneEnvironment();
        if (rest.Count == 0)
            choicePoints.Push(new ChoicePoint(choice, env));
        else
            choicePoints.Push(new ChoicePoint(And(rest.Prepend(choice).ToArray()), env));
    }
    private void Initialize()
    {
        State = VMState.Ready;
        rest = new();
        Environment = new();
        cutIndex = 0;
    }
    private void Backtrack()
    {
        while (cutIndex < choicePoints.Count)
        {
            State = VMState.Success;
            var choicePoint = choicePoints.Pop();
            Substitution.Pool.Release(Environment);
            Environment = choicePoint.Environment;
            choicePoint.Continue(this);
            SuccessToSolution();
        }
    }
    private bool BacktrackOnce()
    {
        if (cutIndex < choicePoints.Count)
        {
            State = VMState.Success;
            var choicePoint = choicePoints.Pop();
            Substitution.Pool.Release(Environment);
            Environment = choicePoint.Environment;
            choicePoint.Continue(this);
            SuccessToSolution();
            return true;
        }
        return false;
    }
    /// <summary>
    /// If no solutions were pushed at the end of the current branch, as signaled by State being Success instead of Solution, 
    /// then the current environment is promoted to a solution.
    /// Allows creating goals that don't push solutions directly, like true/0, or !/0, or unify/2.
    /// This is effectively an optimization since, otherwise, those goals would have to push "empty" solutions 
    /// that would get merged immediately afterwards. 
    /// For example, unify works directly on the environment and sets the state to either success or failure,
    /// but in principle it could also push a solution containing the new substitutions plus the environment at that time (wasteful).
    /// </summary>
    private void SuccessToSolution()
    {
        if (State == VMState.Success)
            Solution();
    }
    private void CleanUp()
    {
        SuccessToSolution();
        Substitution.Pool.Release(Environment);
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
    #region Control Flow
    public static Op NoOp => _ => { };
    public static Op Solve(ITerm goal) => vm =>
    {
        if (!vm.KnowledgeBase.GetMatches(new("__X"), goal, false).TryGetValue(out var matches))
        {
            vm.Fail();
            return;
        }
        vm.PushChoice(Or(matches.Select(m => Solve(m.Predicate.Body)).ToArray()));
        var goalEnum = matches.GetEnumerator();
        NextGoal(vm);
        void NextGoal(ErgoVM vm)
        {
            if (goalEnum.MoveNext())
            {
                vm.Solution(goalEnum.Current.Substitutions);
                var rest = Solve(goalEnum.Current.Predicate.Body);
                if (!goalEnum.Current.Predicate.Body.IsEmpty)
                    vm.PushChoice(NextGoal);
            }
            else
            {
                vm.Fail();
            }
        }
    };
    public static Op Solve(NTuple goals)
    {
        var Ops = goals.Contents.Select(Solve).ToArray();
        return And(Ops);
    }
    public static Op And(params Op[] goals) => vm =>
    {
        vm.rest = new(goals);
        while (vm.rest.TryDequeue(out var goal))
        {
            goal(vm);
            switch (vm.State)
            {
                case VMState.Fail: return;
                case VMState.Solution: vm.MergeEnvironment(); break;
            }
        }
        vm.Solution();
    };
    public static Op Or(params Op[] branches) => vm =>
    {
        if (branches.Length == 0)
            return;
        if (branches.Length == 1)
        {
            branches[0](vm);
            return;
        }
        for (int i = branches.Length - 1; i >= 1; i--)
        {
            vm.PushChoice(branches[i]);
        }
        branches[0](vm);
        vm.SuccessToSolution();
    };
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
    public static Op Unify(ITerm left, ITerm right) => vm =>
    {
        // Unification doesn't produce a solution, it simply updates the environment
        if (Substitution.Unify(new(left, right)).TryGetValue(out var subs))
        {
            vm.Environment.AddRange(subs);
            Substitution.Pool.Release(subs);
        }
        else
            vm.Fail();
    };
    #endregion
}
