namespace Ergo.Lang;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class TermAttribute : Attribute
{
    public TermMarshalling Marshalling { get; set; } = TermMarshalling.Positional;
    public string Functor { get; set; } = null;
}
