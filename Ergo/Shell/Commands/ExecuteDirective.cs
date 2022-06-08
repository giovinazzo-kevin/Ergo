using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class ExecuteDirective : ShellCommand
    {
        public ExecuteDirective()
            : base(new[] { ":-", "←" }, "", @"(?<dir>.*)", true, 15)
        {
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            var dir = m.Groups["dir"].Value;
            var interpreterScope = scope.InterpreterScope;
            var currentModule = interpreterScope.Modules[scope.InterpreterScope.Module];
            var parsed = shell.Parse<Directive>(scope, $":- {(dir.EndsWith('.') ? dir : dir + '.')}").Value;
            var directive = parsed.GetOrDefault();
            if (shell.Interpreter.RunDirective(ref interpreterScope, directive))
            {
                return scope.WithInterpreterScope(interpreterScope);
            }
            else throw new ShellException($"'{dir}' does not resolve to a directive.");
        }
    }
}
