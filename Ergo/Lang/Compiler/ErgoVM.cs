using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class ErgoVM
{
    public static void NoOp() { } // unit action
    public readonly record struct ChoicePoint(Action Invoke, SubstitutionMap Environment);

    // Temporary, these two properties will be removed in due time.
    public SolverContext Context { get; set; }
    public SolverScope Scope { get; set; }
    public KnowledgeBase KnowledgeBase { get; set; }

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
    public Action Goal(ITerm goal) => () =>
    {
        if (!KnowledgeBase.GetMatches(new("__X"), goal, false).TryGetValue(out var matches))
        {
            Fail();
            return;
        }
        Solution();
        PushChoice(Or(matches.Select(m => Goals(m.Predicate.Body)).ToArray()));

        var goalEnum = matches.GetEnumerator();
        NextGoal();
        void NextGoal()
        {
            if (goalEnum.MoveNext())
            {
                Solution(goalEnum.Current.Substitutions);
                var rest = Goals(goalEnum.Current.Predicate.Body);
                if (!goalEnum.Current.Predicate.Body.IsEmpty)
                    PushChoice(NextGoal);
            }
            else
            {
                Fail();
            }
        }
    };
    public Action Goals(NTuple goals)
    {
        var actions = goals.Contents.Select(Goal).ToArray();
        return And(actions);
    }
    public Action And(params Action[] goals) => () =>
    {
        rest = new(goals);
        while (rest.TryDequeue(out var goal))
        {
            var nSols = solutions.Count;
            goal();
            if (failure)
                return;
            // If a goal doesn't fail and doesn't produce solutions, that means it's a built-in such as unification or cut.
            // There's no need to merge the environments since there is no solution to pop.
            if (nSols == solutions.Count)
                continue;
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
        // If the query was a builtin like unify that doesn't produce solutions on its own, return the current environment
        if (!failure && solutions.Count == 0)
            Solution();
        else Substitution.Pool.Release(Environment);
    }
}
