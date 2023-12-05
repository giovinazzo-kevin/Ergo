using Ergo.Events.Runtime;
using System.Collections;
using System.Diagnostics;
using System.IO;

using GeneratorDef = (int NumSolutions, System.Collections.Generic.IEnumerable<Ergo.Runtime.Solution> Solutions);

namespace Ergo.Runtime;

[Flags]
public enum VMFlags
{
    Default = EnableInlining | EnableOptimizations,
    None = 0,
    EnableInlining = 1,
    EnableOptimizations = 4
}

public sealed class Solutions : IEnumerable<Solution>
{
    public delegate IEnumerable<Solution> Generator(int num);

    private readonly List<GeneratorDef> generators = new();
    public int Count { get; private set; }

    public void Clear()
    {
        generators.Clear();
        Count = 0;
    }

    public void Push(Generator gen, int num)
    {
        if (num <= 0)
            return;
        Count += num;
        generators.Add((num, gen(num)));
    }

    public void Push(SubstitutionMap subs)
    {
        Push(_ => Enumerable.Empty<Solution>().Append(new(subs)), 1);
    }

    public Maybe<Solution> Pop()
    {
        if (Count == 0)
            return default;
        Count--;
        var gc = generators.Count - 1;
        var ret = generators[gc];
        generators.RemoveAt(gc);
        if (ret.NumSolutions == 1)
            return ret.Solutions.Single();
        var sol = ret.Solutions.Last();
        generators.Add((ret.NumSolutions - 1, ret.Solutions.SkipLast(1)));
        return sol;
    }

    public void Discard(int num)
    {
    _Repeat:
        if (Count == 0)
            return;
        var ret = generators[Count - 1];
        var sols = ret.Solutions;
        while (num-- > 0 && ret.NumSolutions > 0)
        {
            sols = sols.SkipLast(1);
        }
        if (num > 0)
        {
            generators.RemoveAt(--Count);
            goto _Repeat;
        }
    }

    public IEnumerator<Solution> GetEnumerator()
    {
        return generators
            .SelectMany(gen => gen.Solutions)
            .GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public sealed class RefCount
{
    private readonly Dictionary<Variable, int> dict = new();
    public int Count(Variable variable)
    {
        if (!dict.TryGetValue(variable, out var count))
            dict[variable] = count = 0;
        return dict[variable] = count + 1;
    }
    public int GetCount(Variable variable)
    {
        if (!dict.TryGetValue(variable, out var count))
            return 0;
        return count;
    }
    public void Clear() => dict.Clear();
}

public partial class ErgoVM
{
    public const int MAX_ARGUMENTS = 255;

    #region Type Declarations
    /// <summary>
    /// Represents any operation that can be invoked against the VM. Ops can be composed in order to direct control flow and capture outside context.
    /// </summary>
    public delegate void Op(ErgoVM vm);
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
    protected Solutions solutions = new();
    protected RefCount refCounts = new();
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
    public ErgoVM(KnowledgeBase kb, VMFlags flags = VMFlags.Default, DecimalType decimalType = DecimalType.CliDecimal)
    {
        args = new ITerm[MAX_ARGUMENTS];
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
    public ErgoVM Clone() => new(KnowledgeBase, Flags, DecimalType)
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
            while (solutions.Pop().TryGetValue(out var sol))
                yield return sol;
            if (!BacktrackOnce())
                break;
        }
        CleanUp();
        while (solutions.Pop().TryGetValue(out var sol))
            yield return sol;
    }
    #endregion
    #region Goal API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg(int index, ITerm value) => args[index] = value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITerm Arg(int index) => args[index];
    public ReadOnlySpan<ITerm> Args => args.AsSpan()[..Arity];
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
    }
    /// <summary>
    /// Uses a solution generator to add a specified number of solutions that will be generated lazily.
    /// </summary>
    public void Solution(Solutions.Generator gen, int count)
    {
        solutions.Push(gen, count);
        State = VMState.Solution;
    }
    /// <summary>
    /// Yields the current environment as a solution.
    /// </summary>
    public void Solution()
    {
        solutions.Push(!Environment.Any() ? SubstitutionMap.Empty : CloneEnvironment());
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
    public bool TryPopSolution(out SubstitutionMap subs)
    {
        subs = default;
        if (solutions.Count == 0 || State != VMState.Solution)
            return false;
        subs = solutions.Pop().GetOrThrow(StackEmptyException).Substitutions;
        State = solutions.Count > 0 ? VMState.Solution : VMState.Success;
        return true;
    }
    public void MergeEnvironment()
    {
        var subs = solutions.Pop().GetOrThrow(StackEmptyException).Substitutions;
        Ops.UpdateEnvironment(subs)(this);
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
    public Op CompileQuery(Query query)
    {
        var exps = GetQueryExpansions(query);
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
