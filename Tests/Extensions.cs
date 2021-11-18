using System.Text.RegularExpressions;

namespace Tests
{
    public static class Extensions
    {
        public static string RemoveExtraWhitespace(this string s) => new Regex("\\s+", RegexOptions.Compiled | RegexOptions.Multiline).Replace(s, " ");
    }
}
