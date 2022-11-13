using Ergo.Solver;

namespace Ergo.Interpreter.Libraries.Tabling;

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
