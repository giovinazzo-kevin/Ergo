using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class BuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;

    public virtual int OptimizationOrder => 0;
    public virtual bool IsDeterminate(ImmutableArray<ITerm> args) => false;

    public virtual ExecutionNode Optimize(BuiltInNode node) => node;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes, OptimizationFlags flags) => nodes;

    public Predicate GetStub(ImmutableArray<ITerm> arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Predicate(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true, default);
    }
    public abstract ErgoVM.Op Compile();
}
