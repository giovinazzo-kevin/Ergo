namespace Ergo.Lang;

public partial class ErgoLexer
{
    public static readonly HashSet<string> TrueSymbols = [
        "true", "⊤"
    ];
    public static readonly HashSet<string> FalseSymbols = [
        "false", "⊥"
    ];
    public static readonly HashSet<string> CutSymbols = [
        "!"
    ];
    public static readonly HashSet<string> BooleanSymbols =
        [.. FalseSymbols
, .. TrueSymbols];
    public static readonly HashSet<string> KeywordSymbols =
        [.. CutSymbols
, .. BooleanSymbols];

    public static readonly HashSet<string> PunctuationSymbols = [
        "(", ")", "[", "]", "{", "}", ",", "."
    ];

    public readonly string[] OperatorSymbols;

}
