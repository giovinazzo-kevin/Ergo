using Ergo.Lang.Ast;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{

    public abstract class AssertShellCommand : ShellCommand
    {
        public readonly bool Start;

        public AssertShellCommand(string[] names, string desc, bool start)
            : base(names, desc, @"(?<predicate>.*)", 10)
        {
            Start = start;
        }

        public override void Callback(ErgoShell s, Match m)
        {
            var parsed = s.Parse<Predicate>(m.Groups["predicate"].Value).Value;
            if (!parsed.HasValue)
            {
                return;
            }
            var pred = parsed.Reduce(some => some, () => default);
            s.ExceptionHandler.Try(() => {
                if (Start)
                {
                    s.Interpreter.AssertA(s.CurrentModule, pred);
                }
                else
                {
                    s.Interpreter.AssertZ(s.CurrentModule, pred);
                }
            });
            s.WriteLine($"Asserted {Predicate.Signature(pred.Head)} at the {(Start ? "beginning" : "end")} of the predicate list.", LogLevel.Inf);
        }
    }
}
