using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Unify : SolverBuiltIn
{
    public Unify()
        : base("", new("unify"), Maybe<int>.Some(2), WellKnown.Modules.Prologue)
    {
    }

    public override Maybe<ExecutionNode> Optimize(BuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        //if (args[0] is Variable { Ignored: true } && args[1] is Variable)
        //    return TrueNode.Instance; // TODO: verify, might be sketchy
        if (!node.Goal.IsGround)
            return node;
        if (args[0].Unify(args[1]).TryGetValue(out _))
            return TrueNode.Instance;
        return FalseNode.Instance;
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        if (arguments[0].Unify(arguments[1]).TryGetValue(out var subs))
            yield return True(subs);
        else
            yield return False();
    }
}
