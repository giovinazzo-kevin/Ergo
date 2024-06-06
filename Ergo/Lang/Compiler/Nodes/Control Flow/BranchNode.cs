namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a logical disjunction.
/// </summary>
public class BranchNode(ExecutionNode left, ExecutionNode right) : ExecutionNode
{
    public readonly ExecutionNode Left = left;
    public readonly ExecutionNode Right = right;

    public override bool IsGround => Left.IsGround && Right.IsGround;
    public override bool IsDeterminate => false;

    public IEnumerable<ExecutionNode> Unfold()
    {
        if (Left is BranchNode bl)
        {
            foreach (var u in bl.Unfold())
                yield return u;
        }
        else yield return Left;
        if (Right is BranchNode br)
        {
            foreach (var u in br.Unfold())
                yield return u;
        }
        else yield return Right;
    }
    public override int CheckSum => HashCode.Combine(Left.CheckSum, Right.CheckSum);

    //public override ErgoVM.Op Compile() => ErgoVM.Ops.Or(Unfold().Select(x => x.Compile()).ToArray());
    public override ErgoVM.Op Compile() => ErgoVM.Ops.Or(Left.Compile(), Right.Compile());
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
        if (IsGround) return this;
        return new BranchNode(Left.Instantiate(ctx, vars), Right.Instantiate(ctx, vars));
    }

    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new BranchNode(Left.Substitute(s), Right.Substitute(s));
    }
    public override string Explain(bool canonical = false) => $"{("( " + Left.Explain(canonical))}\r\n{("; " + Right.Explain(canonical)).Indent(1)} )";
    public override void Analyze()
    {
        Left.Analyze();
        Right.Analyze();
    }
}
