using Ergo.Lang.Ast.Terms.Interfaces;
using System.Collections.Concurrent;

namespace Ergo.Lang.Utils;

// TODO: Refactor into non static class
internal static class AbstractTermCache
{
    private static readonly ConcurrentDictionary<Type, HashSet<int>> Misses = new();
    private static readonly ConcurrentDictionary<int, IAbstractTerm> Cache = new();

    public static void Set(ITerm t, IAbstractTerm a)
    {
        var key = t.GetHashCode();
        if (Cache.ContainsKey(key))
            throw new InvalidOperationException();
        Cache[key] = a;
    }

    public static void Miss(ITerm t, Type type)
    {
        if (!Misses.TryGetValue(type, out var set))
            set = Misses[type] = new();
        set.Add(t.GetHashCode());
    }
    public static bool IsNot(ITerm t, Type type) => Misses.TryGetValue(type, out var set) && set.Contains(t.GetHashCode());
    public static Maybe<IAbstractTerm> Get(ITerm t, Type checkType)
    {
        if (Cache.TryGetValue(t.GetHashCode(), out var x) && x.GetType().Equals(checkType))
        {
            return Maybe.Some(x);
        }

        return default;
    }
}
