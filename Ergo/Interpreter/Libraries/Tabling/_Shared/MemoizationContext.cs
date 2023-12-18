using Ergo.Runtime;

namespace Ergo.Interpreter.Libraries.Tabling;

public readonly struct VariantKey
{
    public readonly int Value = 0;
    public VariantKey(ITerm from)
    {
        Value = 1;
    }
}

public sealed class MemoizationContext
{
    private Dictionary<ITerm, MemoizationTable> MemoizationTable = new();

    public void MemoizePioneer(ITerm pioneer)
    {
        if (!MemoizationTable.TryGetValue(pioneer, out _))
            MemoizationTable[pioneer] = new();
        else throw new InvalidOperationException();
    }

    public void MemoizeFollower(ITerm pioneer, ITerm follower)
    {
        if (!MemoizationTable.TryGetValue(pioneer, out var tbl))
            throw new InvalidOperationException();
        tbl.Followers.Add(follower);
    }

    public void MemoizeSolution(ITerm pioneer, Solution sol)
    {
        if (!MemoizationTable.TryGetValue(pioneer, out var tbl))
            throw new InvalidOperationException();
        sol.Substitutions.Prune(pioneer.Variables);
        tbl.Solutions.Add(sol);
    }

    public Maybe<ITerm> GetPioneer(ITerm variant)
    {
        var key = MemoizationTable.Keys.FirstOrDefault(k => variant.IsVariantOf(k));
        if (key != null)
            return Maybe.Some(key);
        return default;
    }

    public IEnumerable<ITerm> GetFollowers(ITerm pioneer)
    {
        if (!MemoizationTable.TryGetValue(pioneer, out var tbl))
            throw new InvalidOperationException();
        return tbl.Followers;
    }

    public IEnumerable<Solution> GetSolutions(ITerm pioneer)
    {
        if (!MemoizationTable.TryGetValue(pioneer, out var tbl))
            throw new InvalidOperationException();
        return tbl.Solutions;
    }

}
