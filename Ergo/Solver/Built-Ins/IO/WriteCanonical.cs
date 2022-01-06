using Ergo.Lang;

namespace Ergo.Solver.BuiltIns
{
    public sealed class WriteCanonical : WriteBuiltIn
    {
        public WriteCanonical()
            : base("", new("write_canonical"), Maybe<int>.Some(1), canon: true, quoted: true)
        {
        }
    }
}
