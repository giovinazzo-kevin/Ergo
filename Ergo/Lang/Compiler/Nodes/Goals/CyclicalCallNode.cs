

namespace Ergo.Lang.Compiler;
public class CyclicalCallNode : DynamicNode
{
    public readonly Signature Signature;
    public CyclicalCallNode(ITerm goal) : base(goal)
    {
        Signature = goal.GetSignature();
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
        => new CyclicalCallNode(Goal.Instantiate(ctx, vars));
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
        => new CyclicalCallNode(Goal.Substitute(s));
}
