using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;

namespace Ergo.Interpreter.Directives
{
    public class DefineLiteral : InterpreterDirective
    {
        public DefineLiteral()
            : base("", new("lit"), Maybe.Some(2))
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (!args[0].Matches<string>(out var literalName))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            scope = scope.WithModule(scope.Modules[scope.CurrentModule]
                .WithLiteral(new(new(literalName), args[1])));
            return true;
        }
    }
}
