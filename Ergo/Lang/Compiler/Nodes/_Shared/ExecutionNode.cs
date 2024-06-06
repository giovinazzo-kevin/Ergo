using System.Diagnostics;

namespace Ergo.Lang.Compiler;

[DebuggerDisplay("{Explain(false)}")]
public abstract class ExecutionNode : IExplainable
{
    public virtual bool IsGround => true;
    public virtual int OptimizationOrder => 0;
    public virtual bool IsDeterminate => false;
    public virtual int CheckSum => 1;
    public bool IsContinuationDet { get; internal set; } = false;
    public virtual void Analyze() { }
    public virtual ExecutionNode Optimize(OptimizationFlags flags) => this;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes, OptimizationFlags flags) => nodes;
    public abstract ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ExecutionNode Substitute(IEnumerable<Substitution> s);
    public abstract ErgoVM.Op Compile();
    public abstract string Explain(bool canonical = false);
}
