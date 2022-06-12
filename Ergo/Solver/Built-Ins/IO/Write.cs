namespace Ergo.Solver.BuiltIns;

public sealed class Write : WriteBuiltIn
{
    public Write()
        : base("", new("write"), Maybe<int>.Some(1), canon: false, quoted: false)
    {
    }
}
