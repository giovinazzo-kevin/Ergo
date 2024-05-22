using System.Collections;

namespace Ergo.Runtime;

public readonly record struct LazySolutionEnumerable(int Count, Func<int, Solution> Sol) : ISolutionEnumerable
{
    public Solution this[int index] => index == 0 ? Sol(index) : throw new ArgumentOutOfRangeException(nameof(index));

    public IEnumerator<Solution> GetEnumerator()
    {
        return Enumerable.Range(0, Count).Select(Sol).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
