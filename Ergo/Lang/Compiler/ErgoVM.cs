using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    public static void NoOp() { } // unit action
    public readonly record struct ChoicePoint(Action Invoke, SubstitutionMap Environment);

    // Temporary, these two properties will be removed in due time.
    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }

    protected Stack<ChoicePoint> choicePoints = new();
    protected Stack<SubstitutionMap> solutions = new();
    protected int cutIndex = int.MaxValue;
    protected bool failure = false;
    protected Queue<Action> rest;

    public SubstitutionMap Environment { get; private set; }
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(Scope, x));

    private Action _query = NoOp;
    public Action Query
    {
        get => _query;
        set => _query = value ?? NoOp;
    }
    public void PushChoice(Action action)
    {
        var env = CloneEnvironment();
        if (rest.Count == 0)
            choicePoints.Push(new ChoicePoint(action, env));
        else
            choicePoints.Push(new ChoicePoint(And(rest.Prepend(action).ToArray()), env));
    }
    public Action And(params Action[] goals) => () =>
    {
        rest = new(goals);
        while (rest.TryDequeue(out var goal))
        {
            goal();
            if (goal == Cut)
                continue;
            if (failure)
                return;
            MergeEnvironment();
        }
        Solution();
    };
    public Action Or(params Action[] branches) => () =>
    {
        if (branches.Length == 0)
            return;
        if (branches.Length == 1)
        {
            branches[0]();
            return;
        }
        for (int i = branches.Length - 1; i >= 1; i--)
        {
            PushChoice(branches[i]);
        }
        branches[0]();
    };
    public Action IfThenElse(Action condition, Action consequence, Action alternative) => () =>
    {
        var backupEnvironment = CloneEnvironment();
        condition();
        Cut();
        if (!failure)
        {
            MergeEnvironment();
            consequence();
        }
        else
        {
            failure = false;
            Environment = backupEnvironment;
            alternative();
        }
    };
    public Action IfThen(Action condition, Action consequence) => IfThenElse(condition, consequence, NoOp);

    public void Fail()
    {
        failure = true;
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop();
        Environment.AddRange(subs);
        Substitution.Pool.Release(subs);
    }
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        solutions.Push(subs);
    }
    public void Solution()
    {
        solutions.Push(CloneEnvironment());
    }
    public void Cut()
    {
        cutIndex = choicePoints.Count;
    }
    public SubstitutionMap CloneEnvironment()
    {
        var env = Substitution.Pool.Acquire();
        env.AddRange(Environment);
        return env;
    }
    private void Initialize()
    {
        rest = new();
        Environment = new();
        cutIndex = 0;
        failure = false;
    }

    public void Run()
    {
        Initialize();
        Query();
        // Backtrack
        while (cutIndex < choicePoints.Count)
        {
            failure = false;
            var choicePoint = choicePoints.Pop();
            Substitution.Pool.Release(Environment);
            Environment = choicePoint.Environment;
            choicePoint.Invoke();
        }
        Substitution.Pool.Release(Environment);
    }
}
