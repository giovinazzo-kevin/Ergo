namespace Ergo.VM.BuiltIns;

public sealed class WriteRaw : WriteBuiltIn
{
    public WriteRaw()
        : base("", new("write_raw"), default, canon: false, quoted: false, portray: false)
    {
    }
}
