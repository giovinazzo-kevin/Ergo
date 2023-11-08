using System.Diagnostics;

namespace Ergo.Lang.Compiler;

[DebuggerDisplay("{Explain(false)}")]
public abstract class ExecutionNode : IExplainable
{
    public virtual int OptimizationOrder => 0;
    public virtual ExecutionNode Optimize() => this;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;
    public abstract ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ExecutionNode Substitute(IEnumerable<Substitution> s);
    public abstract Action Compile(ErgoVM vm);
    public abstract string Explain(bool canonical = false);
}
