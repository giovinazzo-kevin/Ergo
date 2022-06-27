namespace Ergo.Solver.BuiltIns;

public sealed class WriteQuoted : WriteBuiltIn
{
    public WriteQuoted()
        : base("", new("writeq"), default, canon: false, quoted: true)
    {
    }
}
