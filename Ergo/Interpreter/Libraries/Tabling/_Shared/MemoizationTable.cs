using Ergo.Solver;

namespace Ergo.Interpreter.Libraries.Tabling;

public sealed class MemoizationTable
{
    public readonly HashSet<ITerm> Followers = new();
    public readonly HashSet<Solution> Solutions = new();

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
