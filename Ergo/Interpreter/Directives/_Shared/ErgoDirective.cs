namespace Ergo.Modules.Directives;

public abstract class ErgoDirective
{
    public readonly record struct Context(ErgoModuleTree ModuleTree, Maybe<Atom> CurrentModuleName)
    {
        public ErgoModule CurrentModule => CurrentModuleName.Map(AccessModule)
            .GetOrThrow(() => new InterpreterException(ErgoInterpreter.ErrorType.UndefinedModule));
        Maybe<ErgoModule> AccessModule(Atom m) => ModuleTree[m];
    }

    public readonly int Priority;
    public readonly string Description;
    public readonly Signature Signature;

    public ErgoDirective(string desc, Atom functor, Maybe<int> arity, int weight)
    {
        Signature = new(functor, arity, Maybe<Atom>.None, Maybe<Atom>.None);
        Description = desc;
        Priority = weight;
    }

    public abstract bool Execute(ref Context context, ImmutableArray<ITerm> args);
}
