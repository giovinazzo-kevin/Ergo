using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    public readonly record struct ChoicePoint(Action Invoke, SubstitutionMap Environment);

    // Temporary, these two properties will be removed in due time.
    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }

    public Stack<ChoicePoint> ChoicePoints = new();
    public Stack<SubstitutionMap> Solutions = new();
    protected int CutIndex = int.MaxValue;
    protected bool Failure = false;

    public SubstitutionMap Environment { get; private set; }
    public bool Success => !Failure;

    static void NoOp() { }
    private Action _query = NoOp;
    public Action Query
    {
        get => _query;
        set => _query = value ?? NoOp;
    }

    public Action And(params Action[] goals) => () =>
    {
        Inner(goals, 0);

        void Inner(Action[] goals, int goalIndex)
        {
            for (int i = goalIndex; i < goals.Length; i++)
            {
                goals[i]();
                if (Failure)
                    return;
                MergeEnvironment();
            }
            Solution();
        }
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
        var backupEnvironment = new SubstitutionMap(Environment);
        var backupChoicePoints = new Stack<ChoicePoint>(ChoicePoints.Reverse());
        condition();
        if (!Failure)
        {
            MergeEnvironment();
            consequence();
        }
        else
        {
            Environment = backupEnvironment;
            ChoicePoints = backupChoicePoints;
            Failure = false;
            alternative();
        }
    };
    public Action IfThen(Action condition, Action consequence) => IfThenElse(condition, consequence, NoOp);

    public void Fail()
    {
        Failure = true;
        Backtrack(); // Go back to the last choice point
    }
    public void MergeEnvironment()
    {
        var subs = Solutions.Pop();
        if (Environment == subs)
            return;
        Environment.AddRange(subs);
        Substitution.Pool.Release(subs);
    }
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        Solutions.Push(subs);
    }
    public void Solution()
    {
        Solutions.Push(new(Environment));
    }
    public void Backtrack()
    {
        while (ChoicePoints.Count >= CutIndex && CutIndex > 0)
        {
            Substitution.Pool.Release(ChoicePoints.Pop().Environment);
        }
    }
    public void Cut()
    {
        CutIndex = ChoicePoints.Count;
    }
    public void PushChoice(Action action)
    {
        ChoicePoints.Push(new(action, new(Environment)));
    }
    private void PushQuery(Action query)
    {
        ChoicePoints.Push(new(query, Substitution.Pool.Acquire()));
    }

    private void Initialize()
    {
        Environment = new();
        CutIndex = int.MaxValue;
        Failure = false;
    }

    public void Run()
    {
        Initialize();
        PushQuery(Query);
        while (ChoicePoints.Count > 0)
        {
            Failure = false;
            var choicePoint = ChoicePoints.Pop();
            Environment = choicePoint.Environment;
            choicePoint.Invoke();
            Backtrack();
        }
    }
}
