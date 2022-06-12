using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Ergo.Lang;

public abstract class ErgoPropertyResolver<T> : ErgoTypeResolver<T>
{
    protected readonly ConcurrentDictionary<string, PropertyInfo> PropertiesByName;
    protected readonly PropertyInfo[] Properties;

    protected static string ToErgoCase(string s)
    {
        // Assume PascalCase
        var wasUpper = true;
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; ++i)
        {
            var isUpper = char.IsUpper(s[i]);
            if (i > 0 && !wasUpper && isUpper)
            {
                sb.Append("_");
            }

            sb.Append(char.ToLower(s[i]));
            wasUpper = isUpper;
        }

        return sb.ToString();
    }

    public ErgoPropertyResolver()
    {
        Properties = (Type.IsArray ? Type.GetElementType() : Type).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();
        PropertiesByName = new(Properties.ToDictionary(p => p.Name));
    }
}
