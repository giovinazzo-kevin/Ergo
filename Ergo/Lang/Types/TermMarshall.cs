using Ergo.Lang.Ast;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ergo.Lang
{
    public sealed class TermMarshall
    {
        internal static readonly ConcurrentDictionary<Type, ITypeResolver> PositionalResolvers = new();
        internal static readonly ConcurrentDictionary<Type, ITypeResolver> NamedResolvers = new();

        internal static ITypeResolver EnsurePositionalResolver(Type t)
        {
            if (!PositionalResolvers.TryGetValue(t, out var resolver))
            {
                resolver = (ITypeResolver)Activator.CreateInstance(typeof(PositionalPropertyTypeResolver<>).MakeGenericType(t));
                PositionalResolvers.AddOrUpdate(t, resolver, (t, r) => r);
            }
            return resolver;
        }

        internal static ITypeResolver EnsureNamedResolver(Type t)
        {
            if (!NamedResolvers.TryGetValue(t, out var resolver))
            {
                resolver = (ITypeResolver)Activator.CreateInstance(typeof(NamedPropertyTypeResolver<>).MakeGenericType(t));
                NamedResolvers.AddOrUpdate(t, resolver, (t, r) => r);
            }
            return resolver;
        }

        private static TermMarshalling GetMode(Type type, TermMarshalling? mode = null) => (mode ?? type.GetCustomAttribute<TermAttribute>()?.Marshalling ?? TermMarshalling.Positional);

        public static ITerm ToTerm<T>(T value, TermMarshalling? mode = null) =>
            GetMode(typeof(T), mode) switch
            {
                TermMarshalling.Positional => EnsurePositionalResolver(typeof(T)).ToTerm(value),
                TermMarshalling.Named => EnsureNamedResolver(typeof(T)).ToTerm(value),
                _ => throw new NotImplementedException()
            };
        public static ITerm ToTerm(object value, Type type, TermMarshalling? mode = null) =>
            GetMode(type, mode) switch
            {
                TermMarshalling.Positional => EnsurePositionalResolver(type).ToTerm(value),
                TermMarshalling.Named => EnsureNamedResolver(type).ToTerm(value),
                _ => throw new NotImplementedException()
            };
        public static T FromTerm<T>(ITerm value, T _ = default, TermMarshalling? mode = null) =>
            GetMode(typeof(T), mode) switch
            {
                TermMarshalling.Positional => (T)Convert.ChangeType(EnsurePositionalResolver(typeof(T)).FromTerm(value), typeof(T)),
                TermMarshalling.Named => (T)Convert.ChangeType(EnsureNamedResolver(typeof(T)).FromTerm(value), typeof(T)),
                _ => throw new NotImplementedException()
            };
        public static object FromTerm(ITerm value, Type type, TermMarshalling? mode = null) =>
            GetMode(type, mode) switch
            {
                TermMarshalling.Positional => EnsurePositionalResolver(type).FromTerm(value),
                TermMarshalling.Named => EnsureNamedResolver(type).FromTerm(value),
                _ => throw new NotImplementedException()
            };
    }
}
