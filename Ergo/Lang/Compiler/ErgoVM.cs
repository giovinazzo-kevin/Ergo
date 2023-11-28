using Ergo.Solver;
using System.Diagnostics;

namespace Ergo.Lang.Compiler;

public partial class ErgoVM
{

    #region Type Declarations
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
    /// <summary>
    /// Represents a parametrizable operation that can be invoked against the VM. Creates a closure over the parameters and returns a parameterless Op.
    /// </summary>
    public delegate Op Goal(ImmutableArray<ITerm> args);
    /// <summary>
    /// Represents a continuation point for the VM to backtrack to and a snapshot of the VM at the time when this choice point was created.
    /// </summary>
    public readonly record struct ChoicePoint(Op Continue, SubstitutionMap Environment);
    #endregion
    // Temporary, these properties will be removed in due time.
    public SolverContext Context { get; set; }
    // Temporary, these properties will be removed in due time.
    public SolverScope Scope { get; set; }
    // Temporary, these properties will be removed in due time.
    public KnowledgeBase KnowledgeBase { get; set; }
    public readonly InstantiationContext InstCtx = new("VM");
    #region Internal VM State
    protected Stack<ChoicePoint> choicePoints = new();
    protected Stack<SubstitutionMap> solutions = new();
    protected int cutIndex;
    public Op @continue;
    #endregion
    #region External VM API
    /// <summary>
    /// Represents the current execution state of the VM.
    /// </summary>
    public VMState State { get; private set; } = VMState.Ready;
    /// <summary>
    /// The active set of substitutions containing the state for the current execution branch.
    /// </summary>
    public SubstitutionMap Environment { get; set; }
    /// <summary>
    /// The current computed set of solutions. See also <see cref="RunInteractive"/>, which yields them one at a time.
    /// </summary>
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(Scope, x));
    /// <summary>
    /// The number of choice points in the stack, which can be used to keep track of choice points created after a particular invocation.
    /// </summary>
    public int NumChoicePoints => choicePoints.Count;
    private Op _query = Ops.NoOp;
    /// <summary>
    /// The entry point for the VM. Set <see cref="Query"/> before calling <see cref="Run"/> or <see cref="RunInteractive"/>. Defaults to <see cref="Ops.NoOp"/>.
    /// </summary>
    public Op Query
    {
        get => _query;
        set => _query = value ?? Ops.NoOp;
    }
    /// <summary>
    /// Executes <see cref="Query"/> and backtracks until all solutions are computed. See also <see cref="Solutions"/> and <see cref="RunInteractive"/>.
    /// </summary>
    public void Run()
    {
        Initialize();
        State = VMState.Success;
        Query(this);
        Backtrack();
        CleanUp();
    }
    /// <summary>
    /// Starting enumeration will cause the VM to run in interactive mode, yielding one solution at a time.  See also <see cref="Solutions"/> and <see cref="Run"/>.
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
    /// <summary>
    /// Sets the VM in a failure state and raises an exception.
    /// </summary>
    public void Throw(ErrorType error, params object[] args)
    {
        State = VMState.Fail;
        throw new RuntimeException(error, args);
    }
    /// <summary>
    /// Sets the VM in a failure state, signalling backtracking.
    /// </summary>
    public void Fail()
    {
        State = VMState.Fail;
    }
    /// <summary>
    /// Yields an external substitution map as a solution.
    /// </summary>
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        solutions.Push(subs);
        State = VMState.Solution;
        LogState();
    }
    /// <summary>
    /// Yields the current environment as a solution.
    /// </summary>
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
            choicePoints.Push(new ChoicePoint(Ops.And2(choice, cont), env));
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
