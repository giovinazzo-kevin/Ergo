namespace Ergo.Lang;

public partial class ErgoLexer
{
    public static readonly HashSet<string> TrueSymbols = new string[] {
        "true", "⊤"
    }.ToHashSet();
    public static readonly HashSet<string> FalseSymbols = new string[] {
        "false", "⊥"
    }.ToHashSet();
    public static readonly HashSet<string> CutSymbols = new string[] {
        "!"
    }.ToHashSet();
    public static readonly HashSet<string> BooleanSymbols =
        FalseSymbols
        .Concat(TrueSymbols)
        .ToHashSet();
    public static readonly HashSet<string> KeywordSymbols =
        CutSymbols
        .Concat(BooleanSymbols)
        .ToHashSet();

    public static readonly HashSet<string> PunctuationSymbols = new string[] {
        "(", ")", "[", "]", "{", "}", ",", "."
    }.ToHashSet();

    public readonly string[] OperatorSymbols;

}
