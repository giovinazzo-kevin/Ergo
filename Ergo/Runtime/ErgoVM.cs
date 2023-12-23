using Ergo.Events.Runtime;
using System.Diagnostics;
using System.IO;
using static Ergo.Runtime.Solutions;

namespace Ergo.Runtime;

[Flags]
public enum CompilerFlags
{
    Default = EnableInlining | EnableOptimizations,
    None = 0,
    EnableInlining = 1,
    EnableOptimizations = 4,
}
[Flags]
public enum VMFlags
{
    None = 0,
    /// <summary>
    /// If set, the rest of the current execution path (@continue) is known to be determinate.
    /// </summary>
    ContinuationIsDet = 1
}

public enum VMMode
{
    /// <summary>
    /// Yields solutions interactively, one at a time. Ideal for a REPL environment.
    /// </summary>
    Interactive,
    /// <summary>
    /// Computes all solutions.
    /// </summary>
    Batch
}

public partial class ErgoVM
{
    public const int MAX_ARGUMENTS = 255;

    #region Type Declarations
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
    public delegate Op Call(ReadOnlySpan<ITerm> args);
    /// <summary>
    /// Represents a continuation point for the VM to backtrack to and a snapshot of the VM at the time when this choice point was created.
    /// </summary>
    public readonly record struct ChoicePoint(Op Continue, SubstitutionMap Environment);
    #endregion
    public readonly DecimalType DecimalType;
    public readonly KnowledgeBase KB;
    public readonly InstantiationContext InstantiationContext = new("VM");
    #region Internal VM State
    protected Stack<ChoicePoint> choicePoints = new();
    protected Solutions solutions = new();
    protected RefCount refCounts = new();
    /// <summary>
    /// Register for all sorts of runtime flags.
    /// </summary>
    protected VMFlags flags;
    /// <summary>
    /// Register for the choice point cut index.
    /// </summary>
    protected int cutIndex;
    /// <summary>
    /// Acts as a register for the current goal's arguments.
    /// </summary>
    protected ITerm[] args;
    /// <summary>
    /// Register for the number of arguments for the current call. Needed by variadics.
    /// </summary>
    public int Arity;
    /// <summary>
    /// Register for the current continuation.
    /// </summary>
    internal Op @continue;
    public ErgoVM(KnowledgeBase kb, DecimalType decimalType = DecimalType.CliDecimal)
    {
        args = new ITerm[MAX_ARGUMENTS];
        KB = kb;
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
    public VMMode Mode { get; private set; } = VMMode.Batch;
    /// <summary>
    /// The active set of substitutions containing the state for the current execution branch.
    /// </summary>
    public SubstitutionMap Environment { get; set; }
    /// <summary>
    /// The current computed set of solutions. See also <see cref="RunInteractive"/>, which yields them one at a time.
    /// </summary>
    public IEnumerable<Solution> Solutions => solutions;
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
    public ErgoVM Clone() => new(KB, DecimalType)
    { In = In, Err = Err, Out = Out, /*args = [.. args], Arity = Arity*/ };
    /// <summary>
    /// Executes <see cref="Query"/> and backtracks until all solutions are computed. See also <see cref="Solutions"/> and <see cref="RunInteractive"/>.
    /// </summary>
    public void Run()
    {
        Initialize();
        Mode = VMMode.Batch;
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
        Mode = VMMode.Interactive;
        State = VMState.Success;
        Query(this);
        while (State != VMState.Ready)
        {
            while (TryPopSolution(out var sol))
                yield return sol;
            if (!BacktrackOnce())
                break;
        }
        CleanUp();
        while (TryPopSolution(out var sol))
            yield return sol;
    }
    #endregion
    #region Goal API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg(int index, ITerm value) => args[index] = value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITerm Arg(int index) => args[index];
    public ReadOnlySpan<ITerm> Args => args.AsSpan()[..Arity];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Flag(VMFlags flag) => flags.HasFlag(flag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(VMFlags flag, bool value)
    {
        flags = value ? flags | flag : flags & ~flag;
    }
    /// <summary>
    /// Sets the VM in a failure state and raises an exception.
    /// </summary>
    public void Throw(ErrorType error, params object[] args)
    {
        State = VMState.Fail;
        KB.Scope.ExceptionHandler.Throw(new RuntimeException(error, args));
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
    /// Sets the VM in a non-failure state that halts early.
    /// </summary>
    public void Ready()
    {
        State = VMState.Ready;
    }
    public void Success()
    {
        State = VMState.Success;
    }
    /// <summary>
    /// Yields an external substitution map as a solution.
    /// </summary>
    public void Solution(SubstitutionMap subs)
    {
        subs.AddRange(Environment);
        solutions.Push(subs);
        State = VMState.Solution;
    }
    /// <summary>
    /// Uses a solution generator to add a specified number of solutions that will be generated lazily.
    /// </summary>
    public GeneratorDef Solution(Generator gen, int count)
    {
        State = VMState.Solution;
        return solutions.Push(gen, count);
    }
    /// <summary>
    /// Yields the current environment as a solution.
    /// </summary>
    public void Solution()
    {
        solutions.Push(CloneEnvironment());
        State = VMState.Solution;
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
            SubstitutionMap.Pool.Release(cp.Environment);
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
    protected void Trace(string text, [CallerMemberName] string caller = null)
    {
        System.Diagnostics.Trace.WriteLine($"{caller}: {text}");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SuccessToSolution()
    {
        if (State == VMState.Success)
            Solution();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPopSolution(out Solution sol)
    {
        sol = default;
        if (solutions.Count == 0)
            return false;
        sol = solutions.Pop().GetOrThrow(StackEmptyException);
        //State = solutions.Count > 0 ? VMState.Solution : VMState.Success;
        return true;
    }
    public void MergeEnvironment()
    {
        if (!TryPopSolution(out var sol))
            throw StackEmptyException;
        Ops.UpdateEnvironment(sol.Substitutions)(this);
        State = VMState.Success;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SubstitutionMap CloneEnvironment() => Environment.Clone();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSingletonVariable(Variable v) => refCounts.GetCount(v) == 1;

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
            SubstitutionMap.Pool.Release(Environment);
            Environment = choicePoint.Environment;
            choicePoint.Continue(this);
            SuccessToSolution();
            return true;
        }
        return false;
    }
    protected virtual void Initialize()
    {
        State = VMState.Ready;
        Environment = [];
        cutIndex = 0;
        @continue = Ops.NoOp;
        refCounts.Clear();
        solutions.Clear();
        choicePoints.Clear();
    }
    protected virtual void CleanUp()
    {
        SuccessToSolution();
        SubstitutionMap.Pool.Release(Environment);
    }
    public Op CompileQuery(Query query, CompilerFlags flags = CompilerFlags.Default)
    {
        var exps = GetQueryExpansions(query, flags);
        var ops = new Op[exps.Length];
        for (int i = 0; i < exps.Length; i++)
        {
            var subs = exps[i].Substitutions; subs.Invert();
            var newPred = exps[i].Predicate.Substitute(subs);
            ops[i] = newPred.ExecutionGraph.TryGetValue(out var graph)
                ? graph.Compile()
                : Ops.NoOp;
        }
        var branch = Ops.Or(ops);
        return vm =>
        {
            foreach (var var in query.Goals.Variables)
                vm.refCounts.Count(var);
            branch(vm);
        };
    }

    KBMatch[] GetQueryExpansions(Query query, CompilerFlags flags)
    {
        var topLevelHead = new Complex(WellKnown.Literals.TopLevel, query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
        var topLevel = new Predicate(string.Empty, KB.Scope.Entry, topLevelHead, query.Goals, dynamic: true, exported: false, tailRecursive: false, graph: default);
        KB.AssertA(topLevel);
        // Let libraries know that a query is being submitted, so they can expand or modify it.
        KB.Scope.ForwardEventToLibraries(new QuerySubmittedEvent(this, query, flags));
        var queryExpansions = KB
            .GetMatches(InstantiationContext, topLevelHead, desugar: false)
            .AsEnumerable()
            .SelectMany(x => x)
            .ToArray();
        KB.RetractAll(topLevelHead);
        return queryExpansions;
    }
}
