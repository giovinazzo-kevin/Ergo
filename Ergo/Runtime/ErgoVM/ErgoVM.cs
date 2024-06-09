using Ergo.Events.Runtime;
using Ergo.Lang.Compiler;
using System.Diagnostics;
using System.IO;
using static Ergo.Runtime.Solutions;

namespace Ergo.Runtime;

[Flags]
public enum CompilerFlags
{
    Default = EnableInlining | EnableOptimizations | PruneIgnoredVariables,
    DefaultNoPrune = Default & ~PruneIgnoredVariables,
    None = 0,
    EnableInlining = 1,
    EnableOptimizations = 4,
    PruneIgnoredVariables = 8
}
[Flags]
public enum VMFlags
{
    None = 0,
    /// <summary>
    /// If set, the rest of the current execution path (@continue) is known to be determinate.
    /// </summary>
    ContinuationIsDet = 1,
    /// <summary>
    /// 
    /// </summary>
    TCO = 2
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
    private static int NUM_VMS = 0;
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
    public readonly record struct ChoicePoint(Op Continue, TermMemory.State State);
    #endregion
    public readonly DecimalType DecimalType;
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
    /// Register for the number of arguments for the current call. Needed by variadics.
    /// </summary>
    public int Arity;
    /// <summary>
    /// Register for the current continuation.
    /// </summary>
    internal Op @continue;

    public readonly TermMemory Memory;
    public readonly CompiledKnowledgeBase CKB;
    protected ITermAddress[] args = new ITermAddress[MAX_ARGUMENTS];
    protected HashSet<VariableAddress> trackedVars;
    #endregion
    #region I/O
    public TextReader In { get; private set; } = Console.In;
    public TextWriter Out { get; private set; } = Console.Out;
    public TextWriter Err { get; private set; } = Console.Error;

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
    public int Id { get; private set; } = NUM_VMS++;
    /// <summary>
    /// Represents the current execution state of the VM.
    /// </summary>
    public VMState State { get; internal set; } = VMState.Ready;
    public VMMode Mode { get; internal set; } = VMMode.Batch;
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
    public ErgoVM ScopedInstance() => new(CKB, DecimalType)
    { In = In, Err = Err, Out = Out, /*args = [.. args], Arity = Arity*/ };

    public void TrackVariable(VariableAddress arg)
    {
        trackedVars.Add(arg);
    }


    /// <summary>
    /// Executes <see cref="Query"/> and backtracks until all solutions are computed. See also <see cref="Solutions"/> and <see cref="RunInteractive"/>.
    /// </summary>
    public void Run()
    {
        Initialize();
        LogState();
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
        LogState();
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
    [Obsolete]
    public IEnumerable<ITerm> DebugArgs => args[..Arity]
        .Select(x => Memory.Dereference(x));

    [Obsolete]
    public void SetArg(int index, ITerm value) => args[index + 1] = Memory.StoreTerm(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg2(int index, ITermAddress value) => args[index] = value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArgs2(ITermAddress[] value)
    {
        Arity = value.Length;
        Array.Copy(value, 0, args, 0, Arity);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete]
    public ITerm Arg(int index) => Memory.Dereference(args[index + 1]);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITermAddress Arg2(int index) => args[index];
    public ReadOnlySpan<ITermAddress> Args2 => args.AsSpan()[..Arity];

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
        LogState();
        State = VMState.Fail;
        CKB.Scope.ExceptionHandler.Throw(new RuntimeException(error, args));
        choicePoints.Clear();
    }
    /// <summary>
    /// Sets the VM in a failure state, signalling backtracking.
    /// </summary>
    public void Fail()
    {
        LogState();
        State = VMState.Fail;
    }
    /// <summary>
    /// Sets the VM in a non-failure state that halts early.
    /// </summary>
    public void Ready()
    {
        LogState();
        State = VMState.Ready;
    }
    public void Success()
    {
        LogState();
        State = VMState.Success;
    }

    public IEnumerable<Substitution> Env => trackedVars
        .Select(x => new Substitution(
            (Variable)Memory.InverseVariableLookup[x],
            Memory.Dereference(x)))
        .Where(x => !x.Lhs.Equals(x.Rhs));

    /// <summary>
    /// Yields an external substitution map as a solution.
    /// </summary>
    [Obsolete]
    public void Solution(SubstitutionMap subs)
    {
        LogState($"Solution (map, {subs.Select(s => s.Explain()).Join("; ")})");
        subs.AddRange(Env);
        solutions.Push(subs);
        State = VMState.Solution;
    }
    /// <summary>
    /// Uses a solution generator to add a specified number of solutions that will be generated lazily.
    /// </summary>
    public GeneratorDef Solution(Generator gen, int count)
    {
        LogState($"Solution (gen, {count})");
        State = VMState.Solution;
        return solutions.Push(gen, count);
    }
    /// <summary>
    /// Yields the current environment as a solution.
    /// </summary>
    public void Solution()
    {
        LogState("Solution (env)");
        solutions.Push(new SubstitutionMap(Env));
        State = VMState.Solution;
    }

    public void Cut()
    {
        LogState();
        if (State != VMState.Fail)
            cutIndex = choicePoints.Count;
    }

    public void SetCutIndex(int index)
    {
        cutIndex = index;
    }

    public Maybe<ChoicePoint> PopChoice()
    {
        if (choicePoints.TryPop(out var ret))
            return ret;
        return default;
    }

    private static readonly Exception StackEmptyException = new RuntimeException(ErrorType.StackEmpty);

    public ErgoVM(CompiledKnowledgeBase kb, DecimalType decimalType = DecimalType.CliDecimal)
    {
        DecimalType = decimalType;
        Memory = kb.Memory;
        CKB = kb;
        CKB.CompileGraph();
    }

    public void DiscardChoices(int numChoices)
    {
        while (numChoices-- > 0)
        {
            var cp = PopChoice().GetOrThrow(StackEmptyException);
            //SubstitutionMap.Pool.Release(cp.Environment);
        }
    }

    public void PushChoice(Op choice)
    {
        var cont = @continue;
        var state = Memory.SaveState();
        if (cont == Ops.NoOp)
            choicePoints.Push(new ChoicePoint(choice, state));
        else
            choicePoints.Push(new ChoicePoint(Ops.And2(choice, cont), state));
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
        LogState();
        if (!TryPopSolution(out var sol))
            throw StackEmptyException;
        foreach (var sub in sol.Substitutions)
        {
            var addr = Memory.StoreVariable(((Variable)sub.Lhs).Name);
            Memory[addr] = Memory.StoreTerm(sub.Rhs);
        }
        State = VMState.Success;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSingletonVariable(Variable v) => refCounts.GetCount(v) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Backtrack()
    {
        while (BacktrackOnce()) ;
    }
    internal virtual bool BacktrackOnce()
    {
        if (cutIndex < choicePoints.Count)
        {
            LogState();
            State = VMState.Success;
            var choicePoint = choicePoints.Pop();
            Memory.LoadState(choicePoint.State);
            choicePoint.Continue(this);
            SuccessToSolution();
            return true;
        }
        return false;
    }
    protected virtual void Initialize()
    {
        LogState();
        State = VMState.Ready;
        trackedVars = [];
        cutIndex = 0;
        @continue = Ops.NoOp;
        refCounts.Clear();
        solutions.Clear();
        choicePoints.Clear();
    }
    protected virtual void CleanUp()
    {
        LogState();
        SuccessToSolution();
    }

    [Conditional("ERGO_VM_DIAGNOSTICS")]
    internal void LogState([CallerMemberName] string state = null)
    {
        Debug.WriteLine($"vm{Id:000}:: {state}".Indent(NumChoicePoints, tabWidth: 1));
    }

    public Op ParseAndCompileQuery(string query, CompilerFlags flags = CompilerFlags.Default)
    {
        if (CKB.Scope.Parse<Query>(query).TryGetValue(out var q))
            return CompileQuery(q, flags);
        return Ops.Fail;
    }

    public Op CompileQuery(Query query, CompilerFlags flags = CompilerFlags.Default)
    {
        LogState();
        var variables = query.Goals.Variables.ToArray();
        return vm =>
        {
            foreach (var var in variables)
            {
                vm.refCounts.Count(var);
                vm.TrackVariable(vm.Memory.StoreVariable(var.Name));
            }
            foreach (var exp in GetQueryExpansions(query, flags))
            {
                exp.Body(vm);
                vm.Backtrack();
            }
        };
    }

    IEnumerable<TermMemory.PredicateCell> GetQueryExpansions(Query query, CompilerFlags flags)
    {
        var topLevelHead = new Complex(WellKnown.Literals.TopLevel, query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
        var topLevel = new Predicate(string.Empty, CKB.Scope.Entry, topLevelHead, query.Goals, dynamic: true, exported: false, tailRecursive: false, graph: default);
        var topLevelAddr = CKB.CompileAndAssertA(topLevel);
        var topLevelHeadAddr = Memory[topLevelAddr].Head;
        // Let libraries know that a query is being submitted, so they can expand or modify it.
        CKB.Scope.ForwardEventToLibraries(new QuerySubmittedEvent(this, query, flags));
        var queryExpansions = CKB
            .GetMatches(topLevelHeadAddr)
            .AsEnumerable()
            .SelectMany(x => x);
        var toRemove = new List<PredicateAddress>();
        foreach (var exp in queryExpansions)
        {
            yield return exp;
            toRemove.Add(exp.Addr);
        }
        CKB.RetractAll(toRemove);
    }
}
