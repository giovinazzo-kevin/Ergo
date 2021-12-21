using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public abstract class ShellCommand
    {
        public readonly string[] Names;
        public readonly string Description;
        public readonly Regex Expression;

        public readonly int Priority;

        public abstract void Callback(Shell s, Match m);

        public ShellCommand(string[] names, string desc, string regex, int priority)
        {
            Names = names;
            Description = desc;
            Priority = priority;
            if (names.Length > 0)
            {
                Expression = new Regex(@$"^\s*(?:{string.Join("|", names.Select(n => Regex.Escape(n)))})\s*{regex}\s*$");
            }
            else
            {
                Expression = new Regex(@$"^\s*{regex}\s*$");
            }
        }
    }
}
