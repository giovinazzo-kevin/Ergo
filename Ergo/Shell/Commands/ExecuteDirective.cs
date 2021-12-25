using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ExecuteDirective : ShellCommand
    {
        public ExecuteDirective()
            : base(new[] { ":-" }, "", @"(?<dir>.*)", 10)
        {
        }

        public override void Callback(ErgoShell s, Match m)
        {
            var dir = m.Groups["dir"].Value;
            var currentModule = s.Interpreter.Modules[ErgoInterpreter.UserModule];
            var parsed = s.Parse<Directive>($":- {(dir.EndsWith('.') ? dir : dir + '.')}").Value;
            var directive = parsed.Reduce(some => some, () => default);
            if (s.Interpreter.RunDirective(directive, ref currentModule, fromCli: true))
            {
                s.CurrentModule = currentModule.Name;
            }
            else throw new ShellException($"'{dir}' does not resolve to a directive.");
        }
    }
}
