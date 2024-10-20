using PeterO.Numbers;

namespace Ergo.Lang;

public sealed class TermMarshallingContext
{
    private static readonly object _syncRoot = new();
    public readonly Stack<object> ReferenceStack = new();
    internal readonly Dictionary<TermMarshalling, Dictionary<int, ITerm>> ToCache = [];

    public bool TryGetCached(TermMarshalling mode, object key, Type type, Maybe<Atom> functor, out ITerm cached)
    {
        cached = default;
        if (key is null)
            return false;
        lock (_syncRoot)
        {
            if (!ToCache.TryGetValue(mode, out var cache))
                ToCache[mode] = cache = [];
            var hashCode = HashCode.Combine(key, type);
            if (cache.TryGetValue(hashCode, out var cached_))
            {
                cached = functor.Map(some => some, cached_.GetFunctor)
                    .Reduce(cached_.WithFunctor, () => cached_);
                return true;
            }
        }
        return false;
    }

    internal ITerm Cache(TermMarshalling mode, object key, Type type, ITerm term)
    {
        lock (_syncRoot)
        {
            if (!ToCache.TryGetValue(mode, out var cache))
                ToCache[mode] = cache = [];
            var hashCode = HashCode.Combine(key, type);
            cache[hashCode] = term;
        }
        return term;
    }
    public static bool MayCauseCycles(Type type)
    {
        if (type.IsClass && !type.IsSealed && !type.IsPrimitive && !type.IsEnum
            && type != typeof(string) && type != typeof(EDecimal))
        {
            return true;
        }
        return false;
    }
}
