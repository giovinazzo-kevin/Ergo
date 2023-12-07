namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents an if-then-else statement.
/// </summary>
public class IfThenElseNode : ExecutionNode
{
    public override bool IsDeterminate => false;

    public IfThenElseNode(ExecutionNode condition, ExecutionNode trueBranch, ExecutionNode falseBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }
    public ExecutionNode FalseBranch { get; }
    public override bool IsGround => Condition.IsGround && TrueBranch.IsGround && FalseBranch.IsGround;

    public override ErgoVM.Op Compile() => ErgoVM.Ops.IfThenElse(Condition.Compile(), TrueBranch.Compile(), FalseBranch.Compile());
    public override ExecutionNode Optimize()
    {
        if (Condition is TrueNode)
            return TrueBranch.Optimize();
        if (Condition is FalseNode)
            return FalseBranch.Optimize();
        return new IfThenElseNode(Condition.Optimize(), TrueBranch.Optimize(), FalseBranch.Optimize());
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new IfThenElseNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars), FalseBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new IfThenElseNode(Condition.Substitute(s), TrueBranch.Substitute(s), FalseBranch.Substitute(s));
    }
    public override string Explain(bool canonical = false) => $"{Condition.Explain(canonical)}\r\n{("-> " + TrueBranch.Explain(canonical)).Indent(1)}\r\n{(" ; " + FalseBranch.Explain(canonical)).Indent(1)}";
}
