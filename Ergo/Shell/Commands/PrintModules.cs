using Ergo.Interpreter;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintModules : ShellCommand
{
    public PrintModules()
        : base(new[] { ":m", "modules" }, "Displays a tree view of the current module and its imports", @"", true, 85)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match match)
    {
        var modules = scope.InterpreterScope.Modules;
        var currentModule = scope.InterpreterScope.Modules[scope.InterpreterScope.Module];
        shell.WriteTree(currentModule,
            x => x.Name,
            x => x.Imports.Contents.Select(i => modules[(Atom)i]),
            x => x.Name.Explain(),
            x => !x.Name.Equals(Modules.Stdlib)
        );
        yield return scope;
    }
}
