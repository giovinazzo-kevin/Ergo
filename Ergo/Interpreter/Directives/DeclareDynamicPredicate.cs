using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;

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
                sig = args[0].GetSignature();
            }
            scope = scope.WithModule(scope.Modules[scope.Module]
                .WithDynamicPredicate(sig));
            return true;
        }
    }
}
