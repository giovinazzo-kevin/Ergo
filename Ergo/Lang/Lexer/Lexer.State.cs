namespace Ergo.Lang;

public partial class Lexer
{
    public readonly ref struct StreamState
    {
        public readonly string Filename;
        public readonly long Position;
        public readonly int Line;
        public readonly int Column;
        public readonly string Context;

        public StreamState(string fn, long pos, int line, int col, string ctx)
        {
            Filename = fn;
            Position = pos;
            Line = line;
            Column = col;
            Context = ctx ?? String.Empty;
        }
    }
}
