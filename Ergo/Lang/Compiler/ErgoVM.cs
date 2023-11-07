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

    public Queue<Action> Rest;

    public SubstitutionMap Environment { get; private set; }
    public bool Success => !Failure;

    public static void NoOp() { }
    private Action _query = NoOp;
    public Action Query
    {
        get => _query;
        set => _query = value ?? NoOp;
    }

    public Action And(params Action[] goals) => () =>
    {
        Rest = new(goals);
        while (Rest.TryDequeue(out var goal))
        {
            goal();
            if (goal == Cut)
            {
                continue;
            }
            if (Failure)
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
        var backupEnvironment = new SubstitutionMap(Environment);
        var backupChoicePoints = new Stack<ChoicePoint>(ChoicePoints.Reverse());
        condition();
        ChoicePoints = backupChoicePoints;
        if (!Failure)
        {
            MergeEnvironment();
            consequence();
        }
        else
        {
            Failure = false;
            Environment = backupEnvironment;
            alternative();
        }
    };
    public Action IfThen(Action condition, Action consequence) => IfThenElse(condition, consequence, NoOp);

    public void Fail()
    {
        Failure = true;
        Backtrack();
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
        if (CutIndex <= 0) return;
        while (CutIndex <= ChoicePoints.Count)
        {
            Substitution.Pool.Release(Environment);
            Environment = ChoicePoints.Pop().Environment;
        }
        CutIndex = int.MaxValue;
    }
    public void Cut()
    {
        CutIndex = ChoicePoints.Count;
    }
    public void PushChoice(Action action)
    {
        if (ChoicePoints.Count >= CutIndex)
        {
            return;
        }

        if (Rest.Count == 0)
            ChoicePoints.Push(new ChoicePoint(action, new SubstitutionMap(Environment)));
        else
        {
            ChoicePoints.Push(new ChoicePoint(And(Rest.Prepend(action).ToArray()), new SubstitutionMap(Environment)));
        }
    }
    private void PushQuery(Action query)
    {
        ChoicePoints.Push(new(query, Substitution.Pool.Acquire()));
    }

    private void Initialize()
    {
        Rest = new();
        Environment = new();
        CutIndex = int.MaxValue;
        Failure = false;
    }

    public void Run()
    {
        Initialize();
        Query();
        while (ChoicePoints.Count > 0)
        {
            Failure = false;
            var choicePoint = ChoicePoints.Pop();
            if (CutIndex > ChoicePoints.Count + 1)
            {
                Environment = choicePoint.Environment;
                choicePoint.Invoke();
            }
        }
    }
}
