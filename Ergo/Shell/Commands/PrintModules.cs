using Ergo.Interpreter;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class PrintModules : ShellCommand
    {
        public PrintModules()
            : base(new[] { ":m", "modules" }, "Displays help about all modules that start with the given string", @"(?<module>[^\s].*)?", true, 70)
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
}
