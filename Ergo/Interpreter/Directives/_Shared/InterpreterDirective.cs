using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Interpreter.Directives
{
    public abstract class InterpreterDirective
    {
        public readonly string Description;
        public readonly Signature Signature;

        public InterpreterDirective(string desc, Atom functor, Maybe<int> arity)
        {
            Signature = new(functor, arity);
            Description = desc;
        }

        public abstract bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args);
    }
}
