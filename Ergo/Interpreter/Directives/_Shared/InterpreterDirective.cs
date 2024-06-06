namespace Ergo.Interpreter.Directives;

public abstract class InterpreterDirective(string desc, Atom functor, Maybe<int> arity, int weight)
{
    public readonly int Priority = weight;
    public readonly string Description = desc;
    public readonly Signature Signature = new(functor, arity, Maybe<Atom>.None, Maybe<Atom>.None);

    public abstract bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args);
}
