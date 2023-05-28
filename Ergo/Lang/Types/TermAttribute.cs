namespace Ergo.Lang;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class TermAttribute : Attribute
{
    public TermMarshalling Marshalling { get; set; } = TermMarshalling.Named;
    public string Functor { get; set; } = null;
    public string Key { get; set; } = null;
}
