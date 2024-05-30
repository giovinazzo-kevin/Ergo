namespace Ergo.Lang.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        if (sequences == null)
            return null;

        IEnumerable<IEnumerable<T>> emptyProduct = [Enumerable.Empty<T>()];

        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) => accumulator.SelectMany(
                accseq => sequence,
                (accseq, item) => accseq.Concat([item])));
    }

    public static Type GetEnumerableType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException("type");

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        var iface = (from i in type.GetInterfaces()
                     where i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                     select i).FirstOrDefault();

        if (iface == null)
            throw new ArgumentException("Does not represent an enumerable type.", "type");

        return GetEnumerableType(iface);
    }

}
