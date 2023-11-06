namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a logical disjunction.
/// </summary>
public class BranchNode : ExecutionNode
{
    public readonly ExecutionNode Left;
    public readonly ExecutionNode Right;

    public BranchNode(ExecutionNode left, ExecutionNode right)
    {
        Left = left;
        Right = right;
    }

    public override Action Compile(ErgoVM vm) => vm.Or(Left.Compile(vm), Right.Compile(vm));
    public override ExecutionNode Optimize()
    {
        var left = Left.Optimize();
        var right = Right.Optimize();
        if (left is FalseNode)
            return right;
        if (right is FalseNode)
            return left;
        return new BranchNode(left, right);
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new BranchNode(Left.Instantiate(ctx, vars), Right.Instantiate(ctx, vars));
    }

    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new BranchNode(Left.Substitute(s), Right.Substitute(s));
    }
    public override string Explain(bool canonical = false) => $"( {Left.Explain(canonical)}\r\n; {Right.Explain(canonical)} )";
}
