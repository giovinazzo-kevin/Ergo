namespace Ergo.Modules.Directives;

public abstract class ErgoDirective
{
    public readonly int Priority;
    public readonly string Description;
    public readonly Signature Signature;

    public ErgoDirective(string desc, Atom functor, Maybe<int> arity, int weight)
    {
        Signature = new(functor, arity, Maybe<Atom>.None, Maybe<Atom>.None);
        Description = desc;
        Priority = weight;
    }

    public abstract bool Execute(ErgoModuleTree moduleTree, ref Maybe<Atom> currentModule, ImmutableArray<ITerm> args);
}
