using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ergo.Lang
{

    public class TypeMarshall
    {
        internal static readonly ConcurrentDictionary<Type, ITypeResolver> Resolvers = new();

        internal static ITypeResolver EnsureResolver<T>()
        {
            if(!Resolvers.TryGetValue(typeof(T), out var resolver))
            {
                Resolvers.AddOrUpdate(typeof(T), resolver = new TypeResolver<T>(), (t, r) => r);
            }
            return resolver;
        }
        public static bool CanMarshall(Type t) => Resolvers.ContainsKey(t);
        public static Term ToTerm<T>(T value) => EnsureResolver<T>().ToTerm(value);
        public static T FromTerm<T>(Term value, T _ = default)=> (T)EnsureResolver<T>().FromTerm(value);
    }
}
