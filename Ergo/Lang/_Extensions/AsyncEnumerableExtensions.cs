namespace Ergo.Lang.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        if (sequences == null)
            return null;

        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };

        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) => accumulator.SelectMany(
                accseq => sequence,
                (accseq, item) => accseq.Concat([item])));
    }
}
