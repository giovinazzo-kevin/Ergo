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

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            var dir = m.Groups["dir"].Value;
            var interpreterScope = scope.InterpreterScope;
            var currentModule = interpreterScope.Modules[scope.InterpreterScope.CurrentModule];
            var parsed = shell.Parse<Directive>(scope, $":- {(dir.EndsWith('.') ? dir : dir + '.')}").Value;
            var directive = parsed.Reduce(some => some, () => default);
            if (shell.Interpreter.RunDirective(ref interpreterScope, directive))
            {
                scope = scope.WithInterpreterScope(interpreterScope);
            }
            else throw new ShellException($"'{dir}' does not resolve to a directive.");
        }
    }
}
