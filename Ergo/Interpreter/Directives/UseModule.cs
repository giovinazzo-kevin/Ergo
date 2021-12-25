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
            : base("", new("use_module"), Maybe.Some(1))
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (args[0] is not Atom moduleName)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            if (moduleName == scope.CurrentModule)
            {
                return false;
            }
            if (!scope.Modules.TryGetValue(moduleName, out var module))
            {
                module = interpreter.Load(ref scope, moduleName.Explain());
            }
            if (module.Imports.Contents.Contains(moduleName))
            {
                return false;
            }
            scope = scope.WithModule(module.WithImport(moduleName));
            return true;
        }
    }
}
