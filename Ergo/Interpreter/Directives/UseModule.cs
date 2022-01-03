using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System;
using System.Linq;

namespace Ergo.Interpreter.Directives
{
    public class UseModule : InterpreterDirective
    {
        public UseModule()
            : base("", new("use_module"), Maybe.Some(1), 1)
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (args[0] is not Atom moduleName)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, Types.String, args[0].Explain());
            }
            if (moduleName == scope.Module || scope.Modules[scope.Module].Imports.Contents.Contains(moduleName))
            {
                return false;
            }
            if (!scope.Modules.TryGetValue(moduleName, out var module))
            {
                var importScope = scope;
                module = ModuleLoader.LoadDirectives(interpreter, ref importScope, moduleName.Explain());
            }
            scope = scope
                .WithModule(module)
                .WithModule(scope.Modules[scope.Module]
                    .WithImport(moduleName));
            return true;
        }
    }
}
