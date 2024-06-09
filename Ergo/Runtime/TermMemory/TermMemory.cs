using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Compiler;

public sealed class TermMemory(int vS = 1024, int sS = 1024, int aS = 1024, int pS = 1024)
{
    public readonly record struct State(
        ITermAddress[] Variables,
        ITermAddress[][] Structures,
        AbstractCell[] Abstracts,
        PredicateCell[] Predicates,
        Dictionary<string, VariableAddress> VariableLookup,
        Dictionary<VariableAddress, string> InverseVariableLookup,
        Dictionary<ITermAddress, PredicateAddress> PredicateLookup,
        Dictionary<int, ITermAddress> TermLookup
    );
    public readonly record struct AbstractCell(IAbstractTermCompiler Compiler, ITermAddress Address, Type Type, bool IsCellDisposed = false);
    public readonly record struct PredicateCell(PredicateAddress Addr, ITermAddress Head, ErgoVM.Op Body, bool IsDynamic, bool IsCellDisposed = false);
    private readonly Dictionary<int, Atom> Atoms = [];
    private readonly ITermAddress[] Variables = new ITermAddress[vS];
    private readonly ITermAddress[][] Structures = new ITermAddress[sS][];
    private readonly AbstractCell[] Abstracts = new AbstractCell[aS];
    private readonly PredicateCell[] Predicates = new PredicateCell[pS];
    public uint VP = 0, SP = 0, AP = 0, PP = 0;
    public readonly int MaxVariables = vS;
    public readonly int MaxStructures = sS;
    public readonly int MaxAbstracts = aS;
    public readonly int MaxPredicates = pS;

    internal Dictionary<string, VariableAddress> VariableLookup = [];
    internal Dictionary<VariableAddress, string> InverseVariableLookup = [];
    internal Dictionary<ITermAddress, PredicateAddress> PredicateLookup = [];
    internal Dictionary<int, ITermAddress> TermLookup = [];

    internal Queue<PredicateAddress> PredicateAddressPool = [];
    internal Queue<VariableAddress> VariablesAddressPool = [];
    internal Queue<StructureAddress> StructuresAddressPool = [];
    internal Queue<AbstractAddress> AbstractsAddressPool = [];

    public IEnumerable<ITerm> StructuresDebug => Enumerable.Range(0, (int)SP)
        .Select(i => new StructureAddress((uint)i))
        .Select(x => x.Deref(this));

    public void Free(VariableAddress var)
    {
        this[var] = var;
    }

    public void Clear()
    {
        VP = 0;
        SP = 0;
        AP = 0;
        Array.Clear(Variables);
        Array.Clear(Structures);
        Array.Clear(Abstracts);
        TermLookup.Clear();
        VariableLookup.Clear();
        InverseVariableLookup.Clear();
        PredicateLookup.Clear();
    }

    public TermMemory Clone()
    {
        var state = SaveState();
        var mem = new TermMemory(MaxVariables, MaxStructures, MaxAbstracts, MaxPredicates);
        mem.LoadState(state);
        return mem;
    }

    public State SaveState()
    {
        var variables = new ITermAddress[VP];
        var structures = new ITermAddress[SP][];
        var abstracts = new AbstractCell[AP];
        var predicates = new PredicateCell[AP];
        Array.Copy(Variables, variables, VP);
        Array.Copy(Abstracts, abstracts, AP);
        Array.Copy(Predicates, predicates, AP);
        for (int i = 0; i < SP; i++)
        {
            structures[i] = new ITermAddress[Structures[i].Length];
            Array.Copy(Structures[i], structures[i], Structures[i].Length);
        }
        return new State(
            variables,
            structures,
            abstracts,
            predicates,
            VariableLookup.ToDictionary(),
            InverseVariableLookup.ToDictionary(),
            PredicateLookup.ToDictionary(),
            TermLookup.ToDictionary()
        );
    }

    public void LoadState(State state)
    {
        (VP, SP, AP, PP) = (
            (uint)state.Variables.Length,
            (uint)state.Structures.Length,
            (uint)state.Abstracts.Length,
            (uint)state.Predicates.Length
        );
        Array.Copy(state.Variables, Variables, VP);
        Array.Copy(state.Structures, Structures, SP);
        Array.Copy(state.Abstracts, Abstracts, AP);
        Array.Copy(state.Predicates, Predicates, AP);
        VariableLookup = state.VariableLookup.ToDictionary();
        InverseVariableLookup = state.InverseVariableLookup.ToDictionary();
        PredicateLookup = state.PredicateLookup.ToDictionary();
        TermLookup = state.TermLookup.ToDictionary();
    }

    public ConstAddress StoreAtom(Atom value)
    {
        var hash = value.GetHashCode();
        var addr = new ConstAddress((uint)hash);
        this[addr] = value;
        return addr;
    }
    public VariableAddress StoreVariable(string name = null)
    {
        if (name != null && VariableLookup.TryGetValue(name, out var v))
            return v;
        if (!VariablesAddressPool.TryDequeue(out var addr))
            addr = new VariableAddress(VP++);
        this[addr] = VariableLookup[name] = addr;
        InverseVariableLookup[addr] = name;
        return addr;
    }
    public StructureAddress StoreStructure(params ITermAddress[] args)
    {
        if (!StructuresAddressPool.TryDequeue(out var addr))
            addr = new StructureAddress(SP++);
        this[addr] = args;
        return addr;
    }
    public AbstractAddress StoreAbstract(AbstractTerm term)
    {
        if (!AbstractsAddressPool.TryDequeue(out var addr))
            addr = new AbstractAddress(AP++);
        this[addr] = new(term.Compiler, term.Compiler.Store(this, term), term.GetType());
        return addr;
    }
    public AbstractAddress StoreAbstract(ITermAddress address, IAbstractTermCompiler compiler)
    {
        if (!AbstractsAddressPool.TryDequeue(out var addr))
            addr = new AbstractAddress(AP++);
        this[addr] = new(compiler, address, compiler.ElementType);
        return addr;
    }
    public PredicateAddress StorePredicate(ITermAddress headAddr, ErgoVM.Op body, bool isDynamic)
    {
        if (!PredicateAddressPool.TryDequeue(out var addr))
            addr = new PredicateAddress(PP++);
        this[addr] = new(addr, headAddr, body, isDynamic);
        return addr;
    }
    public bool Free(ITermAddress addr) => addr switch
    {
        ConstAddress a => FreeConstant(a),
        VariableAddress a => FreeVariable(a),
        StructureAddress a => FreeStructure(a),
        AbstractAddress a => FreeAbstract(a),
        PredicateAddress a => FreePredicate(a),
        _ => throw new NotSupportedException()
    };
    public bool FreeConstant(ConstAddress addr) => Atoms.Remove((int)addr.Index);
    public bool FreeVariable(VariableAddress addr)
    {
        VariablesAddressPool.Enqueue(addr);
        VariableLookup.Remove(InverseVariableLookup[addr]);
        InverseVariableLookup.Remove(addr);
        Variables[addr.Index] = null;
        return true;
    }
    public bool FreeStructure(StructureAddress addr)
    {
        StructuresAddressPool.Enqueue(addr);
        ref var args = ref Structures[addr.Index];
        foreach (var a in args)
            Free(a);
        args = null;
        return true;
    }
    public bool FreePredicate(PredicateAddress addr)
    {
        ref var cell = ref this[addr];
        if (cell.IsCellDisposed)
            throw new ObjectDisposedException(nameof(cell));
        cell = cell with { IsCellDisposed = true };
        Free(cell.Head);
        PredicateAddressPool.Enqueue(addr);
        return true;
    }
    public bool FreeAbstract(AbstractAddress addr)
    {
        ref var cell = ref this[addr];
        if (cell.IsCellDisposed)
            throw new ObjectDisposedException(nameof(cell));
        cell = cell with { IsCellDisposed = true };
        Free(cell.Address);
        AbstractsAddressPool.Enqueue(addr);
        return true;
    }
    public bool IsVariableAssigned(VariableAddress a) => !(this[a] is VariableAddress b && b.Index == a.Index);
    public Atom this[ConstAddress c]
    {
        get => Atoms[(int)c.Index];
        internal set => Atoms[(int)c.Index] = value;
    }
    public ref ITermAddress this[VariableAddress c]
    {
        get => ref Variables[c.Index];
    }
    public ref ITermAddress[] this[StructureAddress c]
    {
        get => ref Structures[c.Index];
    }
    public ref AbstractCell this[AbstractAddress c]
    {
        get => ref Abstracts[c.Index];
    }
    public ref PredicateCell this[PredicateAddress c]
    {
        get => ref Predicates[c.Index];
    }
}

