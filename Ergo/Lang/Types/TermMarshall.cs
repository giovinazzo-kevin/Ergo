using System.Collections.Concurrent;
using System.Reflection;

namespace Ergo.Lang;

public sealed class TermMarshall
{
    public readonly record struct Unmarshalled(object Value);

    internal static readonly ConcurrentDictionary<Type, TermAttribute> AttributeCache = new();
    internal static readonly ConcurrentDictionary<Type, ITermConverter> PositionalResolvers = new();
    internal static readonly ConcurrentDictionary<Type, ITermConverter> NamedResolvers = new();
    internal static readonly ConcurrentDictionary<Type, List<Func<object, object>>> Transforms = new();

    public static Action RegisterTransform<T>(Func<T, T> transform)
    {
        if (!Transforms.TryGetValue(typeof(T), out var list))
            Transforms[typeof(T)] = list = new();
        var cast = (object x) => (object)transform((T)x);
        list.Add(cast);
        return () => list.Remove(cast);
    }

    public static void RegisterConverter(ITermConverter converter)
    {
        if (converter.Marshalling == TermMarshalling.Named)
            NamedResolvers.TryAdd(converter.Type, converter);
        else
            PositionalResolvers.TryAdd(converter.Type, converter);
    }

    internal static ITermConverter EnsurePositionalResolver(Type t)
    {
        var baseType = t;
        do
        {
            if (PositionalResolvers.TryGetValue(baseType, out var pResolver))
                return pResolver;
            baseType = baseType.BaseType;
        }
        while (baseType != typeof(object));
        var resolver = (ITermConverter)Activator.CreateInstance(typeof(PositionalPropertyTypeResolver<>).MakeGenericType(t));
        PositionalResolvers.AddOrUpdate(t, resolver, (t, r) => r);
        return resolver;
    }

    internal static ITermConverter EnsureNamedResolver(Type t)
    {
        var baseType = t;
        do
        {
            if (NamedResolvers.TryGetValue(baseType, out var nResolver))
                return nResolver;
            baseType = baseType.BaseType;
        }
        while (baseType != typeof(object));
        var resolver = (ITermConverter)Activator.CreateInstance(typeof(NamedPropertyTypeResolver<>).MakeGenericType(t));
        NamedResolvers.AddOrUpdate(t, resolver, (t, r) => r);
        return resolver;
    }

    private static TermAttribute GetAttribute(Type type)
    {
        if (type.IsArray)
            type = type.GetElementType();
        if (AttributeCache.TryGetValue(type, out var attribute)) return attribute;
        return AttributeCache[type] = type.GetCustomAttribute<TermAttribute>();
    }

    private static TermMarshalling GetMode(Type type, Maybe<TermMarshalling> mode = default) => mode
        .GetOr(GetAttribute(type)?.Marshalling ?? TermMarshalling.Positional);


    public static ITerm ToTerm<T>(T value, Maybe<Atom> functor = default, Maybe<TermMarshalling> mode = default, TermMarshallingContext ctx = null)
    {
        if (value is Unmarshalled)
            return new Atom(value);
        if (typeof(T) == typeof(ITerm))
            return (ITerm)value;
        ctx ??= new();
        if (value is IErgoMarshalling<T> marshalling)
            return marshalling.ToTerm();
        return GetMode(typeof(T), mode) switch
        {
            var m when ctx.TryGetCached(m, value, typeof(T), functor, out var cached) => cached,
            TermMarshalling.Positional => ctx.Cache(TermMarshalling.Positional, value, typeof(T), EnsurePositionalResolver(typeof(T)).ToTerm(value, functor, default, ctx)),
            TermMarshalling.Named => ctx.Cache(TermMarshalling.Named, value, typeof(T), EnsureNamedResolver(typeof(T)).ToTerm(value, functor, default, ctx)),
            _ => throw new NotImplementedException()
        };
    }
    public static ITerm ToTerm(object value, Type type, Maybe<Atom> functor = default, Maybe<TermMarshalling> mode = default, TermMarshallingContext ctx = null)
    {
        if (value is Unmarshalled)
            return new Atom(value);
        if (type == typeof(ITerm))
            return (ITerm)value;
        ctx ??= new();
        var interfaceType = typeof(IErgoMarshalling<>).MakeGenericType(type);
        if (type.GetInterfaces().Contains(interfaceType))
            return (ITerm)interfaceType.GetMethod(nameof(ToTerm))
                .Invoke(value, Array.Empty<object>());
        return GetMode(type, mode) switch
        {
            var m when ctx.TryGetCached(m, value, type, functor, out var cached) => cached,
            TermMarshalling.Positional => ctx.Cache(TermMarshalling.Positional, value, type, EnsurePositionalResolver(type).ToTerm(value, functor, default, ctx)),
            TermMarshalling.Named => ctx.Cache(TermMarshalling.Named, value, type, EnsureNamedResolver(type).ToTerm(value, functor, default, ctx)),
            _ => throw new NotImplementedException()
        };
    }
    internal static object Transform(object o)
    {
        if (o is null) return null;
        foreach (var (key, list) in Transforms)
        {
            if (key.IsAssignableFrom(o.GetType()))
            {
                foreach (var tx in list)
                {
                    o = tx(o);
                }
            }
        }
        return o;
    }
    public static T FromTerm<T>(ITerm value, T _ = default, Maybe<TermMarshalling> mode = default)
    {
        if (value is Atom { Value: Unmarshalled { Value: var v } })
            return (T)v;
        if (typeof(T).IsAssignableFrom(typeof(ITerm)))
            return (T)value;
        var interfaceType = typeof(IErgoMarshalling<>).MakeGenericType(typeof(T));
        if (typeof(T).GetInterfaces().Contains(interfaceType))
            return (T)interfaceType.GetMethod(nameof(FromTerm))
                .Invoke(_ ?? Activator.CreateInstance<T>(), new object[] { value });
        return GetMode(typeof(T), mode) switch
        {
            TermMarshalling.Positional => (T)Transform(Convert.ChangeType(EnsurePositionalResolver(typeof(T)).FromTerm(value), typeof(T))),
            TermMarshalling.Named => (T)Transform(Convert.ChangeType(EnsureNamedResolver(typeof(T)).FromTerm(value), typeof(T))),
            _ => throw new NotImplementedException()
        };
    }
    public static object FromTerm(ITerm value, Type type, Maybe<TermMarshalling> mode = default)
    {
        if (value is Atom { Value: Unmarshalled { Value: var v } })
            return v;
        if (type == typeof(ITerm))
            return value;
        var interfaceType = typeof(IErgoMarshalling<>).MakeGenericType(type);
        if (type.GetInterfaces().Contains(interfaceType))
            return interfaceType.GetMethod(nameof(FromTerm))
                .Invoke(Activator.CreateInstance(type), new object[] { value });
        return GetMode(type, mode) switch
        {
            TermMarshalling.Positional => Transform(EnsurePositionalResolver(type).FromTerm(value)),
            TermMarshalling.Named => Transform(EnsureNamedResolver(type).FromTerm(value)),
            _ => throw new NotImplementedException()
        };
    }
}
