using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    public readonly record struct ChoicePoint(Action Invoke, SubstitutionMap Environment, Stack<Action> Continuations);

    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }

    private Stack<ChoicePoint> choicePoints = new();
    private Stack<SubstitutionMap> solutions = new();
    private int cutIndex = int.MaxValue;
    private bool failure;

    public SubstitutionMap Environment { get; private set; }
    public bool Success => !failure;
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(Scope, x));

    static void NoOp() { }
    private Action query = NoOp;
    public Action Query
    {
        get => query;
        set => query = value ?? NoOp;
    }

    public Action And(Stack<Action> goalStack) => () =>
    {
        while (goalStack.Any())
        {
            var currentGoal = goalStack.Pop();
            currentGoal();
            if (failure)
                return;
            MergeEnvironment();
        }
        Solution();
    };
    public Action And(params Action[] goals) => () =>
    {
        var goalStack = new Stack<Action>(goals.Reverse());
        And(goalStack)();
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
        var backupChoicePoints = new Stack<ChoicePoint>(choicePoints.Reverse());
        condition();
        if (!failure)
        {
            MergeEnvironment();
            consequence();
        }
        else
        {
            Environment = backupEnvironment;
            choicePoints = backupChoicePoints;
            failure = false;
            alternative();
        }
    };
    public Action IfThen(Action condition, Action consequence) => IfThenElse(condition, consequence, NoOp);
    public void ContinueWith(Action next)
    {
        if (choicePoints.Any())
        {
            choicePoints.Peek().Continuations.Push(next);
        }
        else
        {
            PushChoice(next);
        }
    }

    public void Continue(int i)
    {
        if (!choicePoints.Any())
            return;
        var choicePoint = choicePoints.Peek();
        if (choicePoint.Continuations.Count > 0)
        {
            And(choicePoint.Continuations.Skip(i).ToArray())();
        }
    }

    public void Fail()
    {
        failure = true;
        Backtrack(); // Go back to the last choice point
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop();
        if (Environment == subs)
            return;
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
        solutions.Push(new(Environment));
    }
    public void Backtrack()
    {
        while (choicePoints.Count >= cutIndex && cutIndex > 0)
        {
            Substitution.Pool.Release(choicePoints.Pop().Environment);
        }
    }
    public void Cut()
    {
        cutIndex = choicePoints.Count;
    }
    public void PushChoice(Action action)
    {
        choicePoints.Push(new(action, new(Environment), new()));
    }
    private void PushQuery(Action query)
    {
        choicePoints.Push(new(query, Substitution.Pool.Acquire(), new()));
    }

    private void Initialize()
    {
        Environment = new();
        cutIndex = int.MaxValue;
        failure = false;
    }
    public void Run()
    {
        Initialize();
        PushQuery(Query);
        while (choicePoints.Count > 0)
        {
            failure = false;
            var choicePoint = choicePoints.Pop();
            Environment = choicePoint.Environment;
            choicePoint.Invoke();
            while (choicePoint.Continuations.Count > 0 && !failure)
            {
                And(choicePoint.Continuations)();
            }
            Backtrack();
        }
    }
}
