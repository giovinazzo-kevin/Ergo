namespace Ergo.Lang.Compiler;

public abstract class StaticNode : ExecutionNode
{
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) => this;
    public override ExecutionNode Substitute(IEnumerable<Substitution> s) => this;
}
