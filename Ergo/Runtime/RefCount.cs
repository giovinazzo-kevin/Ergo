namespace Ergo.Runtime;

public sealed class RefCount
{
    private readonly Dictionary<Variable, int> dict = [];
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
