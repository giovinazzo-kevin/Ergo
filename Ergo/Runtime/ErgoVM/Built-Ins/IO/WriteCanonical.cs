namespace Ergo.Runtime.BuiltIns;

public sealed class WriteCanonical : WriteBuiltIn
{
    public WriteCanonical()
        : base("", new("write_canonical"), default, canon: true, quoted: true, portray: true)
    {
    }
}
