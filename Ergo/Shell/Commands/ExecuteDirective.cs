using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
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

        public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            var dir = m.Groups["dir"].Value;
            var interpreterScope = scope.InterpreterScope;
            var currentModule = interpreterScope.Modules[scope.InterpreterScope.Module];
            var parsed = shell.Parse<Directive>(scope, $":- {(dir.EndsWith('.') ? dir : dir + '.')}").Value;
            var directive = parsed.GetOrDefault();
            var success = scope.ExceptionHandler.TryGet(scope, () => shell.Interpreter.RunDirective(ref interpreterScope, directive), out var ret);
            if (success && ret)
            {
                yield return scope.WithInterpreterScope(interpreterScope);
                yield break;
            }
            else if (!success)
            {
                yield break;
            }
            scope.ExceptionHandler.Throw(scope, new ShellException($"'{dir}' does not resolve to a directive."));
        }
    }
}
