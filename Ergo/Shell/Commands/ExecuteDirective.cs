using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

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
        var currentModule = interpreterScope.EntryModule;
        var directive = shell.Parse<Directive>(scope, $":- {(dir.EndsWith('.') ? dir : dir + '.')}").Value.GetOrThrow(new InvalidOperationException());
        var ret = scope.InterpreterScope.ExceptionHandler.TryGet(() => shell.Interpreter.RunDirective(ref interpreterScope, directive));
        if (ret.GetOr(false))
        {
            yield return scope.WithInterpreterScope(interpreterScope);
            yield break;
        }
        else if (!ret.TryGetValue(out _))
        {
            yield break;
        }

        scope.Throw($"'{dir}' does not resolve to a directive.");
    }
}
