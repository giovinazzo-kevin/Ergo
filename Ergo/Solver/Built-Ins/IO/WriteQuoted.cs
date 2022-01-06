using Ergo.Lang;

namespace Ergo.Solver.BuiltIns
{
    public sealed class WriteQuoted : WriteBuiltIn
    {
        public WriteQuoted()
            : base("", new("writeq"), Maybe<int>.Some(1), canon: false, quoted: true)
        {
        }
    }
}
