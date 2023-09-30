using Ergo.Lang.Ast.Terms.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ergo.Lang.Utils;

// TODO: Refactor into non singleton class. But figure out how to pass it around to static extension methods.
public class AbstractTermCache
{
    private readonly ConcurrentDictionary<Type, HashSet<int>> Misses = new();
    private readonly ConcurrentDictionary<int, IAbstractTerm> Cache = new();
    private readonly ConcurrentDictionary<Atom, Type> Index = new();

    public static readonly AbstractTermCache Default = new();

    public void Set(ITerm t, IAbstractTerm a)
    {
        var key = t.GetHashCode();
        if (Cache.ContainsKey(key))
            throw new InvalidOperationException();
        Cache[key] = a;
        if (t.GetFunctor().TryGetValue(out var functor))
            Register(functor, a.GetType());
    }

    public void Register(Atom functor, Type type)
    {
        Index[functor] = type;
    }

    public void Register<T>(Atom functor)
        where T : IAbstractTerm
    {
        Index[functor] = typeof(T);
    }

    public void Miss(ITerm t, Type type)
    {
        if (!Misses.TryGetValue(type, out var set))
            set = Misses[type] = new();
        lock (set)
            set.Add(t.GetHashCode());
    }
    public bool IsNot(ITerm t, Type type)
    {
        if (!Misses.TryGetValue(type, out var set))
            return false;
        lock (set)
            return set.Contains(t.GetHashCode());
    }
    public Maybe<IAbstractTerm> Get(ITerm t, Maybe<Type> type)
    {
        var indexed = false;
        if (!type.TryGetValue(out var checkType))
        {
            if (!t.GetFunctor().TryGetValue(out var functor)
                || !Index.TryGetValue(functor, out checkType))
            {
                return default;
            }
            indexed = true;
        }

        if (Cache.TryGetValue(t.GetHashCode(), out var x) && x.GetType().Equals(checkType))
        {
            return Maybe.Some(x);
        }

        if (indexed)
        {
            return Parse(t, checkType)
                .Do(some => Set(t, some));
        }

        return default;
    }

    protected Maybe<IAbstractTerm> Parse(ITerm t, Type type)
    {
        // If the abstract type implements a static Maybe<T> FromCanonical(ITerm t) method, try calling it and caching the result.
        var resultType = typeof(Maybe<>).MakeGenericType(type);
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
            m.Name.Equals("FromCanonical") && m.GetParameters().Length == 1 && m.ReturnType.Equals(resultType)) is { } unfold)
        {
            var result = unfold.Invoke(null, new[] { t });
            if (resultType.GetField("HasValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(result) is not true)
            {
                Miss(t, type);
                return default;
            }

            var match = (IAbstractTerm)resultType.GetMethod("GetOrThrow").Invoke(result, new object[] { null });
            Set(t, match);
            return Maybe.Some(match);
        }

        return default;
    }

    public Maybe<IAbstractTerm> IsAbstract(ITerm t, Maybe<Type> maybeType)
    {
        // It could be that 't' is the canonical form of an abstract term but that it wasn't recognized as abstract on creation.
        // It's a bit unclear when exactly this happens, but two reasons are:
        // - When parsing the canonical form of an abstract type;
        // - When unifying an abstract term with a matching non-abstract canonical form.
        return t.AbstractForm
            .Where(a => a.GetType().Equals(maybeType))
            .Or(() => Maybe.Some(t)
                .Where(t => maybeType.Reduce(some => !IsNot(t, some), () => true))
                .Map(t => Get(t, maybeType)))
            .Or(() => maybeType.Map(type => Parse(t, type), () => default));
    }
}
