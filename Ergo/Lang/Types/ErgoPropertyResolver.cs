using System.Collections.Concurrent;
using System.Reflection;

namespace Ergo.Lang;

public abstract class ErgoPropertyResolver<T> : ErgoTypeResolver<T>
{
    protected readonly ConcurrentDictionary<string, PropertyInfo> PropertiesByName;
    protected readonly ConcurrentDictionary<string, TermAttribute> AttributesByName;
    protected readonly PropertyInfo[] Properties;
    protected readonly TermAttribute[] Attributes;

    public ErgoPropertyResolver()
    {
        Properties = (Type.IsArray ? Type.GetElementType() : Type)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<NonTermAttribute>() == null)
            .ToArray();
        Attributes = new TermAttribute[Properties.Length];
        PropertiesByName = new(Properties.ToDictionary(p => p.Name));
        int a = 0;
        AttributesByName = new(Properties.ToDictionary(p => p.Name, p =>
        {
            return Attributes[a++] = p.GetCustomAttribute<TermAttribute>() ?? p.PropertyType.GetCustomAttribute<TermAttribute>();
        }));
    }
}
