namespace Ergo.Lang.Compiler;

public class IfThenNode : ExecutionNode
{
    public IfThenNode(ExecutionNode condition, ExecutionNode trueBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }
    public override bool IsGround => Condition.IsGround && TrueBranch.IsGround;
    public override ErgoVM.Op Compile() => ErgoVM.Ops.IfThen(Condition.Compile(), TrueBranch.Compile());
    public override ExecutionNode Optimize()
    {
        if (Condition is TrueNode)
            return TrueBranch.Optimize();
        if (Condition is FalseNode)
            return Condition;
        return new IfThenNode(Condition.Optimize(), TrueBranch.Optimize());
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
