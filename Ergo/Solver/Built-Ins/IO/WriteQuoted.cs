namespace Ergo.Solver.BuiltIns;

public sealed class WriteQuoted : WriteBuiltIn
{
    public WriteQuoted()
        : base("", new("write_quoted"), default, canon: false, quoted: true, portray: true)
    {
    }
}
