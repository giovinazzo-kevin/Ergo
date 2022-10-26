namespace Ergo.Lang;

public partial class ErgoLexer
{
    public readonly record struct StreamState(string Filename, long Position, int Line, int Column, string Context);
}
