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

    public static bool TryGet(ITerm t, out IAbstractTerm a) => Cache.TryGetValue(t.GetHashCode(), out a);
    public static bool TryGet<T>(ITerm t, out T a) where T : IAbstractTerm
    {
        a = default;
        if (Cache.TryGetValue(t.GetHashCode(), out var x) && x is T)
        {
            a = (T)x;
            return true;
        }

        return false;
    }
    public static bool TryGet(ITerm t, Type checkType, out IAbstractTerm a)
    {
        a = default;
        if (Cache.TryGetValue(t.GetHashCode(), out var x) && x.GetType().Equals(checkType))
        {
            a = x;
            return true;
        }

        return false;
    }
}
