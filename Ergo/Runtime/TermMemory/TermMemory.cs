using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Compiler;

public readonly record struct TrailRecord(VariableAddress Lhs, ITermAddress OldRhs, ITermAddress NewRhs);
public sealed class Trail : Stack<TrailRecord>
{

}

public sealed class TermMemory(int cS = 1024, int vS = 1024, int sS = 1024, int aS = 1024, int pS = 1024)
{
    public readonly record struct Binding(VariableAddress Lhs, ITermAddress NewRhs, ITermAddress OldRhs);
    public readonly record struct State(uint CP, uint VP, uint SP, uint AP, uint PP, int TP);
    public readonly record struct AbstractCell(IAbstractTermCompiler Compiler, ITermAddress Address, Type Type, bool IsCellDisposed = false);
    public readonly record struct PredicateCell(PredicateAddress Addr, ITermAddress Head, ErgoVM.Op Body, bool IsTailRecursive, bool IsDynamic, bool IsCellDisposed = false);
    private readonly Atom[] Atoms = new Atom[cS];
    private readonly ITermAddress[] Variables = new ITermAddress[vS];
    private readonly ITermAddress[][] Structures = new ITermAddress[sS][];
    private readonly AbstractCell[] Abstracts = new AbstractCell[aS];
    private readonly PredicateCell[] Predicates = new PredicateCell[pS];
    public uint CP = 0, VP = 0, SP = 0, AP = 0, PP = 0;
    public readonly int MaxAtoms = cS;
    public readonly int MaxVariables = vS;
    public readonly int MaxStructures = sS;
    public readonly int MaxAbstracts = aS;
    public readonly int MaxPredicates = pS;

    internal Dictionary<(Type Type, int HashCode), AtomAddress> AtomLookup = [];
    internal Dictionary<string, VariableAddress> VariableLookup = [];
    internal Dictionary<VariableAddress, string> InverseVariableLookup = [];
    internal Dictionary<ITermAddress, PredicateAddress> PredicateLookup = [];

    internal Queue<AtomAddress> AtomsAddressPool = [];
    internal Queue<PredicateAddress> PredicateAddressPool = [];
    internal Queue<VariableAddress> VariablesAddressPool = [];
    internal Queue<StructureAddress> StructuresAddressPool = [];
    internal Queue<AbstractAddress> AbstractsAddressPool = [];

    internal Stack<Binding> Trail = [];

    public IEnumerable<ITerm> StructuresDebug => Enumerable.Range(0, (int)SP)
        .Select(i => new StructureAddress((uint)i))
        .Select(x => x.Deref(this));

    public void UndoTrail(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!Trail.TryPop(out var t))
                throw new InvalidOperationException();
            ref var cell = ref this[t.Lhs];
            cell = t.Lhs;
            // Debug.Assert(Equals(cell, t.NewRhs));
            // cell = t.OldRhs;
        }
    }

    public State SaveState()
    {
        return new(CP, VP, SP, AP, PP, Trail.Count);
    }
    public void LoadState(State s)
    {
        (CP, VP, SP, AP, PP, var TP) = (s.CP, s.VP, s.SP, s.AP, s.PP, s.TP);
        UndoTrail(Trail.Count - TP);
    }
    public void Clear()
    {
        CP = 0;
        VP = 0;
        SP = 0;
        AP = 0;
        PP = 0;

        Array.Clear(Atoms);
        Array.Clear(Variables);
        Array.Clear(Structures);
        Array.Clear(Abstracts);
        Array.Clear(Predicates);

        AtomLookup.Clear();
        VariableLookup.Clear();
        InverseVariableLookup.Clear();
        PredicateLookup.Clear();

        AtomsAddressPool.Clear();
        PredicateAddressPool.Clear();
        VariablesAddressPool.Clear();
        StructuresAddressPool.Clear();
        AbstractsAddressPool.Clear();
    }

    public AtomAddress StoreAtom(Atom value)
    {
        var lookupKey = (value.Value.GetType(), value.GetHashCode());
        if (AtomLookup.TryGetValue(lookupKey, out var lookupAddr))
            return lookupAddr;
        if (!AtomsAddressPool.TryDequeue(out var addr))
            addr = new AtomAddress(CP++);
        AtomLookup[lookupKey] = addr;
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
    public PredicateAddress StorePredicate(ITermAddress headAddr, ErgoVM.Op body, bool isTailRecursive, bool isDynamic)
    {
        if (!PredicateAddressPool.TryDequeue(out var addr))
            addr = new PredicateAddress(PP++);
        this[addr] = new(addr, headAddr, body, isTailRecursive, isDynamic);
        return addr;
    }
    public bool Free(ITermAddress addr) => addr switch
    {
        AtomAddress a => FreeConstant(a),
        VariableAddress a => FreeVariable(a),
        StructureAddress a => FreeStructure(a),
        AbstractAddress a => FreeAbstract(a),
        PredicateAddress a => FreePredicate(a),
        _ => throw new NotSupportedException()
    };
    public bool FreeConstant(AtomAddress addr)
    {
        AtomsAddressPool.Enqueue(addr);
        var val = Atoms[addr.Index];
        AtomLookup.Remove((val.Value.GetType(), val.GetHashCode()));
        Atoms[addr.Index] = default;
        return true;
    }
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
    public ref Atom this[AtomAddress c]
    {
        get => ref Atoms[c.Index];
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

