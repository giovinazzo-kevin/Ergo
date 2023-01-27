namespace Ergo.Lang;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class NonTermAttribute : Attribute
{
}
