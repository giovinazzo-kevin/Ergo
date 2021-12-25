using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public abstract class RetractShellCommand : ShellCommand
    {
        public readonly bool All;

        public RetractShellCommand(string[] names, string desc, bool all)
            : base(names, desc, @"(?<predicate>.*)", 10)
        {
            All = all;
        }

        public override void Callback(ErgoShell s, Match m)
        {
            var parsed = s.Parse<ITerm>(m.Groups["predicate"].Value).Value;
            if (!parsed.HasValue)
            {
                return;
            }
            var t = parsed.Reduce(some => some, () => default);
            s.ExceptionHandler.Try(() => {
                if (All)
                {
                    if (s.Interpreter.RetractAll(ErgoInterpreter.UserModule, t) is { } delta && delta > 0)
                    {
                        s.WriteLine($"Retracted {delta} predicates that matched with {t}.", LogLevel.Inf);
                    }
                    else
                    {
                        s.No();
                    }
                }
                else
                {
                    if (s.Interpreter.RetractOne(ErgoInterpreter.UserModule, t))
                    {
                        s.Yes();
                    }
                    else
                    {
                        s.No();
                    }
                }
            });
        }
    }
}
