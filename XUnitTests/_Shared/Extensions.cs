using System.Text.RegularExpressions;

namespace Tests;

public static class Extensions
{
    static readonly Regex RemoveExtraWhitespaceRegex = new("\\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    public static string RemoveExtraWhitespace(this string s) => RemoveExtraWhitespaceRegex.Replace(s, " ");
}