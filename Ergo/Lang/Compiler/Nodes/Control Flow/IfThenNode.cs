namespace Ergo.Lang.Compiler;

public class IfThenNode(ExecutionNode condition, ExecutionNode trueBranch) : ExecutionNode
{
    public ExecutionNode Condition { get; } = condition;
    public ExecutionNode TrueBranch { get; } = trueBranch;
    public override bool IsGround => Condition.IsGround && TrueBranch.IsGround;
    public override int CheckSum => HashCode.Combine(Condition.CheckSum, TrueBranch.CheckSum);
    public override ErgoVM.Op Compile() => ErgoVM.Ops.IfThen(Condition.Compile(), TrueBranch.Compile());
    public override ExecutionNode Optimize(OptimizationFlags flags)
    {
        if (Condition is TrueNode)
            return TrueBranch.Optimize(flags);
        if (Condition is FalseNode)
            return Condition;
        return new IfThenNode(Condition.Optimize(flags), TrueBranch.Optimize(flags));
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new IfThenNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new IfThenNode(Condition.Substitute(s), TrueBranch.Substitute(s));
    }
    public override string Explain(bool canonical = false) => $"{Condition.Explain(canonical)}\r\n{("-> " + TrueBranch.Explain(canonical)).Indent(1)}";
    public override void Analyze()
    {
        Condition.Analyze();
        TrueBranch.Analyze();
    }
}
