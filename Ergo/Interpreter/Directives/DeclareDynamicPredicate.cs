using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;

namespace Ergo.Interpreter.Directives
{
    public class DeclareDynamicPredicate : InterpreterDirective
    {
        public DeclareDynamicPredicate()
            : base("", new("dynamic"), Maybe.Some(1), 30)
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if(!Signature.TryUnfold(args[0], out var sig))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Signature, args[0].Explain());
            }
            scope = scope.WithModule(scope.Modules[scope.Module]
                .WithDynamicPredicate(sig));
            return true;
        }
    }
}
