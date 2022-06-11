using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Linq;

namespace Ergo.Interpreter.Directives
{
    public abstract class InterpreterDirective
    {
        public readonly int Priority;
        public readonly string Description;
        public readonly Signature Signature;

        public InterpreterDirective(string desc, Atom functor, Maybe<int> arity, int weight)
        {
            Signature = new(functor, arity, Maybe<Atom>.None, Maybe<Atom>.None);
            Description = desc;
            Priority = weight;
        }

        public abstract bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args);
    }
}
