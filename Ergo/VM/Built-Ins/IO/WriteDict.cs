namespace Ergo.VM.BuiltIns;

public sealed class WriteDict : WriteBuiltIn
{
    public WriteDict()
        : base("", new("write_dict"), default, canon: false, quoted: false, portray: false)
    {
    }

    protected override string Explain(ITerm arg)
    {
        if (arg is Dict dict)
            return dict.Explain(Canonical);
        return AsQuoted(arg, Quoted).Explain(Canonical);
    }

    public static string FormatDict(string json, string indent = "  ")
    {
        var indentation = 0;
        var quoteCount = 0;
        var escapeCount = 0;

        var result =
            from ch in json ?? string.Empty
            let escaped = (ch == '\\' ? escapeCount++ : escapeCount > 0 ? escapeCount-- : escapeCount) > 0
            let quotes = (ch == '"' || ch == '\'') && !escaped ? quoteCount++ : quoteCount
            let unquoted = quotes % 2 == 0
            let colon = ch == ':' && unquoted ? ": " : null
            let nospace = char.IsWhiteSpace(ch) && unquoted ? string.Empty : null
            let lineBreak = ch == ',' && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, indentation)) : null
            let openChar = (ch == '{' || ch == '[') && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, ++indentation)) : ch.ToString()
            let closeChar = (ch == '}' || ch == ']') && unquoted ? Environment.NewLine + string.Concat(Enumerable.Repeat(indent, --indentation)) + ch : ch.ToString()
            select colon ?? nospace ?? lineBreak ?? (
                openChar.Length > 1 ? openChar : closeChar
            );

        return string.Concat(result);
    }

    protected override string TransformText(string text) => FormatDict(text);
}
