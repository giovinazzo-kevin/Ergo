namespace Ergo.Lang;
public sealed class InstantiationContext
{
    public readonly string VarPrefix;
    private long _GlobalVarCounter;
    public InstantiationContext(string prefix) => VarPrefix = prefix;
    public long GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
    public Variable GetFreeVariable() => new Variable($"_{VarPrefix}{GetFreeVariableId():X}");
    public InstantiationContext Clone() => new(VarPrefix) { _GlobalVarCounter = _GlobalVarCounter };
}

