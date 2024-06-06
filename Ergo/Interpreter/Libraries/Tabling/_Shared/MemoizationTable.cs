using Ergo.Runtime;

namespace Ergo.Interpreter.Libraries.Tabling;

public sealed class MemoizationTable
{
    public readonly HashSet<ITerm> Followers = [];
    public readonly HashSet<Solution> Solutions = [];

    public void AddFollowers(IEnumerable<ITerm> followers)
    {
        foreach (var fol in followers)
        {
            Followers.Add(fol);
        }
    }
    public void AddSolutions(IEnumerable<Solution> solutions)
    {
        foreach (var sol in solutions)
        {
            Solutions.Add(sol);
        }
    }
}
