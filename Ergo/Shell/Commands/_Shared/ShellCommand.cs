using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public abstract class ShellCommand
{
    public readonly string[] Names;
    public readonly string Description;
    public readonly Regex Expression;

    public readonly int Priority;

    public abstract IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match match);

    public ShellCommand(string[] names, string desc, string regex, bool optionalRegex, int priority, bool caseInsensitive = false)
    {
        Names = names;
        Description = desc;
        Priority = priority;
        if (names.Length > 0)
        {
            Expression = new Regex(@$"^\s*(?:{names.Join(n => Regex.Escape(n), "|")}){(optionalRegex ? "\\s*" : "\\s+")}{regex}\s*$",
                (caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.Compiled) | RegexOptions.Compiled);

            if (!optionalRegex)
            {
                Expression = new Regex(@$"^\s*(?:{names.Join(n => Regex.Escape(n), "|")})\s*$");
            }
        }
        else
        {
            Expression = new Regex(@$"^\s*{regex}\s*$");
        }
    }
}
