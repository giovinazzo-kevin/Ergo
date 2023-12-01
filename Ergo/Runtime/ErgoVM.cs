using Ergo.Events.Runtime;
using System.Diagnostics;
using System.IO;

namespace Ergo.Runtime;

[Flags]
public enum VMFlags
{
    Default = EnableInlining | EnableOptimizations,
    None = 0,
    EnableInlining = 1,
    EnableOptimizations = 4
}

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
    public VMFlags Flags { get; set; }
    public DecimalType DecimalType { get; set; }
    public readonly KnowledgeBase KnowledgeBase;
    public readonly InstantiationContext InstantiationContext = new("VM");
    #region Internal VM State
    protected Stack<ChoicePoint> choicePoints = new();
    protected Stack<SubstitutionMap> solutions = new();
    protected int cutIndex;
    internal Op @continue;
    public ErgoVM(KnowledgeBase kb, VMFlags flags = VMFlags.Default, DecimalType decimalType = DecimalType.CliDecimal)
    {
        KnowledgeBase = kb;
        Flags = flags;
        DecimalType = decimalType;
        In = Console.In;
        Out = Console.Out;
        Err = Console.Error;
    }
    #endregion
    #region I/O
    public TextReader In { get; private set; }
    public TextWriter Out { get; private set; }
    public TextWriter Err { get; private set; }

    public void SetIn(TextReader newIn)
    {
        ArgumentNullException.ThrowIfNull(newIn);
        In = TextReader.Synchronized(newIn);
    }

    public void SetOut(TextWriter newOut)
    {
        ArgumentNullException.ThrowIfNull(newOut);
        Out = TextWriter.Synchronized(newOut);
    }

    public void SetErr(TextWriter newErr)
    {
        ArgumentNullException.ThrowIfNull(newErr);
        Err = TextWriter.Synchronized(newErr);
    }
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
    public IEnumerable<Solution> Solutions => solutions.Reverse().Select(x => new Solution(x));
    public int NumSolutions => solutions.Count;
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
    /// Creates a new ErgoVM instance that shares the same knowledge base as the current one.
    /// </summary>
    public ErgoVM CreateChild() => new(KnowledgeBase, Flags, DecimalType)
    { In = In, Err = Err, Out = Out };
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
                yield return new Solution(sol);
            if (!BacktrackOnce())
                break;
        }
        CleanUp();
        while (solutions.TryPop(out var sol))
            yield return new Solution(sol);
    }
    #endregion
    #region Goal API
    /// <summary>
    /// Sets the VM in a failure state and raises an exception.
    /// </summary>
    public void Throw(ErrorType error, params object[] args)
    {
        State = VMState.Fail;
        KnowledgeBase.Scope.ExceptionHandler.Throw(new RuntimeException(error, args));
        choicePoints.Clear();
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SuccessToSolution()
    {
        if (State == VMState.Success)
            Solution();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPopSolution(out SubstitutionMap subs)
    {
        subs = default;
        if (solutions.Count == 0 || State != VMState.Solution)
            return false;
        subs = solutions.Pop();
        State = solutions.Count > 0 ? VMState.Solution : VMState.Success;
        return true;
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop();
        Ops.UpdateEnvironment(subs)(this);
        State = VMState.Success;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SubstitutionMap CloneEnvironment() => Environment.Clone();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public Op CompileQuery(Query query)
    {
        var exps = GetQueryExpansions(query);
        var ops = new Op[exps.Length];
        for (int i = 0; i < exps.Length; i++)
        {
            var subs = exps[i].Substitutions; subs.Invert();
            var newPred = exps[i].Predicate.Substitute(subs);
            if (newPred.ExecutionGraph.TryGetValue(out var graph))
            {
                var goal = graph.Compile();
                var op = goal(newPred.Head.GetArguments());
                ops[i] = op;
            }
            else ops[i] = Ops.NoOp;
        }
        return Ops.Or(ops);

        KBMatch[] GetQueryExpansions(Query query)
        {
            var topLevelHead = new Complex(WellKnown.Literals.TopLevel, query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
            var topLevel = new Predicate(string.Empty, KnowledgeBase.Scope.Entry, topLevelHead, query.Goals, dynamic: true, exported: false, tailRecursive: false, graph: default);
            KnowledgeBase.AssertA(topLevel);
            // Let libraries know that a query is being submitted, so they can expand or modify it.
            KnowledgeBase.Scope.ForwardEventToLibraries(new QuerySubmittedEvent(this, query, Flags));
            var queryExpansions = KnowledgeBase
                .GetMatches(InstantiationContext, topLevelHead, desugar: false)
                .AsEnumerable()
                .SelectMany(x => x)
                .ToArray();
            KnowledgeBase.RetractAll(topLevelHead);
            return queryExpansions;
        }
    }
}
