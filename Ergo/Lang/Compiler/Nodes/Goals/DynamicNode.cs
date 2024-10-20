﻿namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a goal that could not be resolved at compile time.
/// </summary>
public class DynamicNode : ExecutionNode
{
    public override bool IsGround => Goal.IsGround;
    public override bool IsDeterminate => IsGround;

    public DynamicNode(ITerm goal)
    {
        Goal = goal;
    }

    public ITerm Goal { get; }
    public override Op Compile() => Ops.Goal(Goal, dynamic: true);

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
