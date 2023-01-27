using System.Collections.Concurrent;
using System.Reflection;

namespace Ergo.Lang;

public abstract class ErgoPropertyResolver<T> : ErgoTypeResolver<T>
{
    protected readonly ConcurrentDictionary<string, PropertyInfo> PropertiesByName;
    protected readonly PropertyInfo[] Properties;

    public ErgoPropertyResolver()
    {
        Properties = (Type.IsArray ? Type.GetElementType() : Type)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<NonTermAttribute>() == null)
            .ToArray();
        PropertiesByName = new(Properties.ToDictionary(p => p.Name));
    }
}
