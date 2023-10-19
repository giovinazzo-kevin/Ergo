namespace Ergo.Lang;
public sealed class InstantiationContext
{
    public readonly string VarPrefix;
    private volatile int _GlobalVarCounter;
    public InstantiationContext(string prefix) => VarPrefix = prefix;
    public int GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
    public InstantiationContext Clone() => new(VarPrefix) { _GlobalVarCounter = _GlobalVarCounter };
}

