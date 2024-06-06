using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Compiler;

public sealed class TermMemory(int vS = 1024, int sS = 1024, int aS = 1024)
{
    public readonly record struct State(
        ITermAddress[] Variables,
        ITermAddress[][] Structures,
        AbstractCell[] Abstracts,
        Dictionary<string, VariableAddress> VariableLookup,
        Dictionary<VariableAddress, string> InverseVariableLookup,
        Dictionary<int, ITermAddress> TermLookup
    );
    public readonly record struct AbstractCell(IAbstractTermCompiler Compiler, ITermAddress Address, Type Type);
    private readonly Dictionary<int, Atom> Atoms = [];
    private readonly ITermAddress[] Variables = new ITermAddress[vS];
    private readonly ITermAddress[][] Structures = new ITermAddress[sS][];
    private readonly AbstractCell[] Abstracts = new AbstractCell[aS];
    public uint VP = 0, SP = 0, AP = 0;

    internal Dictionary<string, VariableAddress> VariableLookup = [];
    internal Dictionary<VariableAddress, string> InverseVariableLookup = [];
    internal Dictionary<int, ITermAddress> TermLookup = [];

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
    }

    public TermMemory Clone()
    {
        var state = SaveState();
        var mem = new TermMemory(vS, sS, aS);
        mem.LoadState(state);
        return mem;
    }

    public State SaveState()
    {
        var variables = new ITermAddress[VP];
        var structures = new ITermAddress[SP][];
        var abstracts = new AbstractCell[AP];
        Array.Copy(Variables, variables, VP);
        Array.Copy(Abstracts, abstracts, AP);
        for (int i = 0; i < SP; i++)
        {
            structures[i] = new ITermAddress[Structures[i].Length];
            Array.Copy(Structures[i], structures[i], Structures[i].Length);
        }
        return new State(variables, structures, abstracts, VariableLookup.ToDictionary(), InverseVariableLookup.ToDictionary(), TermLookup.ToDictionary());
    }

    public void LoadState(State state)
    {
        (VP, SP, AP) = (
            (uint)state.Variables.Length,
            (uint)state.Structures.Length,
            (uint)state.Abstracts.Length
        );
        Array.Copy(state.Variables, Variables, VP);
        Array.Copy(state.Structures, Structures, SP);
        Array.Copy(state.Abstracts, Abstracts, AP);
        VariableLookup = state.VariableLookup.ToDictionary();
        InverseVariableLookup = state.InverseVariableLookup.ToDictionary();
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
        var addr = new VariableAddress(VP++);
        this[addr] = VariableLookup[name] = addr;
        InverseVariableLookup[addr] = name;
        return addr;
    }
    public StructureAddress StoreStructure(params ITermAddress[] args)
    {
        var addr = new StructureAddress(SP++);
        this[addr] = args;
        return addr;
    }
    public AbstractAddress StoreAbstract(AbstractTerm term)
    {
        var addr = new AbstractAddress(AP++);
        this[addr] = new(term.Compiler, term.Compiler.Store(this, term), term.GetType());
        return addr;
    }
    public AbstractAddress StoreAbstract(ITermAddress address, IAbstractTermCompiler compiler)
    {
        var addr = new AbstractAddress(AP++);
        this[addr] = new(compiler, address, compiler.ElementType);
        return addr;
    }
    public Atom this[ConstAddress c]
    {
        get => Atoms[(int)c.Index];
        internal set => Atoms[(int)c.Index] = value;
    }
    public ITermAddress this[VariableAddress c]
    {
        get => Variables[c.Index];
        internal set => Variables[c.Index] = value;
    }
    public ITermAddress[] this[StructureAddress c]
    {
        get => Structures[c.Index];
        internal set => Structures[c.Index] = value;
    }
    public AbstractCell this[AbstractAddress c]
    {
        get => Abstracts[c.Index];
        internal set => Abstracts[c.Index] = value;
    }
}

