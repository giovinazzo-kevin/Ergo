namespace Ergo.Lang;

public partial class ErgoLexer
{
    public readonly struct StreamState
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
            Context = ctx ?? string.Empty;
        }

        public override string ToString() => (Filename, Position, Line, Column, Context).ToString();
    }
}
