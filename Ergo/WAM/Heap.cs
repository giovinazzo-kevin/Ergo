namespace Ergo.WAM;

public readonly struct HeapRef
{
    public readonly ulong Value;
    public bool IsNull() => Value == 0;

    public HeapRef(ulong val) => Value = val;
}

public enum HeapTag : byte
{
    REF,
    STR,
    CON,
    LIS
}

public readonly struct VariableCell
{
    public readonly HeapTag Tag;
    public readonly HeapRef Ref;
}

public sealed class Heap
{
    private readonly VariableCell[] _cells;

    public Heap(long size) => _cells = new VariableCell[size];
}
