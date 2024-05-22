using System.Collections;

namespace Ergo.Runtime;

public readonly record struct SingleSolutionEnumerable(Solution Sol) : ISolutionEnumerable
{
    public Solution this[int index] => index == 0 ? Sol : throw new ArgumentOutOfRangeException(nameof(index));
    public int Count => 1;

    public IEnumerator<Solution> GetEnumerator()
    {
        yield return Sol;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
