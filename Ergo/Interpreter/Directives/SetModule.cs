using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;

namespace Ergo.Interpreter.Directives
{

    public class SetModule : InterpreterDirective
    {
        public SetModule()
            : base("", new("module"), Maybe.Some(1))
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (args[0] is not Atom moduleName)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            var module = interpreter.EnsureModule(ref scope, moduleName);
            scope = scope
                .WithoutModules()
                .WithModule(module)
                .WithCurrentModule(module.Name);
            return true;
        }
    }
}
