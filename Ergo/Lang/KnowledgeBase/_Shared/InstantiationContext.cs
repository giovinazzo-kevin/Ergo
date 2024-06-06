namespace Ergo.Lang;
public sealed class InstantiationContext(string prefix)
{
    public readonly string VarPrefix = prefix;
    private long _GlobalVarCounter;

    public long GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
    public Variable GetFreeVariable() => (Variable)$"_{VarPrefix}{GetFreeVariableId():X}";
    public InstantiationContext Clone() => new(VarPrefix) { _GlobalVarCounter = _GlobalVarCounter };
}

