namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a goal that could not be resolved at compile time.
/// </summary>
public class DynamicNode : ExecutionNode
{
    public DynamicNode(ITerm goal)
    {
        Goal = goal;
    }

    public ITerm Goal { get; }
    public override ErgoVM.Op Compile()
    {
        return ErgoVM.Ops.Goal(Goal);
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new DynamicNode(Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new DynamicNode(Goal.Substitute(s));
    }

    public override string Explain(bool canonical = false) => $"{GetType().Name} ({Goal.Explain(canonical)})";
}
