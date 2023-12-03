using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class BuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;

    public virtual int OptimizationOrder => 0;

    public virtual ExecutionNode Optimize(BuiltInNode node) => node;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;
    public abstract ErgoVM.Op Compile();
}
