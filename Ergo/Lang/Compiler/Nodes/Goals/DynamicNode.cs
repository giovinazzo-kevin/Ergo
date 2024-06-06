namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a goal that could not be resolved at compile time.
/// </summary>
public class DynamicNode(ITerm goal) : ExecutionNode
{
    public override bool IsGround => Goal.IsGround;
    public override bool IsDeterminate => IsGround;

    public ITerm Goal { get; } = goal;
    public override ErgoVM.Op Compile() => vm => ErgoVM.Ops.Goal(vm.Memory.StoreTerm(Goal))(vm);

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new DynamicNode(Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new DynamicNode(Goal.Substitute(s));
    }

    public override string Explain(bool canonical = false) => $"{Goal.Explain(canonical)}";
}
