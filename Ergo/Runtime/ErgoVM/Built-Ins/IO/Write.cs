namespace Ergo.Runtime.BuiltIns;

public sealed class Write : WriteBuiltIn
{
    public Write()
        : base("", new("write"), default, canon: false, quoted: false, portray: true)
    {
    }
}
