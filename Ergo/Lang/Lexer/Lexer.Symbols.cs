namespace Ergo.Lang;

public partial class ErgoLexer
{
    public static readonly string[] TrueSymbols = new string[] {
        "true", "⊤"
    };
    public static readonly string[] FalseSymbols = new string[] {
        "false", "⊥"
    };
    public static readonly string[] CutSymbols = new string[] {
        "!"
    };
    public static readonly string[] BooleanSymbols =
        FalseSymbols
        .Concat(TrueSymbols)
        .ToArray();
    public static readonly string[] KeywordSymbols =
        CutSymbols
        .Concat(BooleanSymbols)
        .ToArray();

    public static readonly string[] PunctuationSymbols = new string[] {
        "(", ")", "[", "]", "{", "}", ",", "."
    };

    public readonly string[] OperatorSymbols;

}
