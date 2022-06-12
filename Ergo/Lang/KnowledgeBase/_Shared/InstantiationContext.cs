using System.Threading;

namespace Ergo.Lang;

public class InstantiationContext
{
    public readonly string VarPrefix;
    private volatile int _GlobalVarCounter;
    public InstantiationContext(string prefix) => VarPrefix = prefix;
    public int GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
}

