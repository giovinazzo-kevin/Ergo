using System.Reflection;

namespace Ergo.Lang;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class TermAttribute : Attribute
{
    public TermMarshalling Marshalling { get; set; } = TermMarshalling.Named;
    public string Functor { get; set; } = null;
    public string Key { get; set; } = null;
    public Type Proxy { get; set; } = null;
    public bool ProxyFunctor { get; set; } = false;
    public bool ProxyMarshalling { get; set; } = false;
    public bool ProxyKey { get; set; } = false;
    public string ComputedFunctor => Functor
        ?? Proxy?.GetCustomAttribute<TermAttribute>()?.ComputedFunctor;
    public string ComputedKey => Key
        ?? Proxy?.GetCustomAttribute<TermAttribute>()?.ComputedKey;
}
