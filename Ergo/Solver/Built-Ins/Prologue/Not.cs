
using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Not : SolverBuiltIn
{
    public Not()
        : base("", new("not"), Maybe<int>.Some(1), WellKnown.Modules.Prologue)
    {
    }

    public override ExecutionNode Optimize(BuiltInNode node)
    {
        if (!node.Goal.IsGround)
            return node;
        var arg = node.Goal.GetArguments()[0].ToExecutionNode(node.Node.Graph, ctx: new("__NOT")).Optimize();
        if (arg is TrueNode)
            return FalseNode.Instance;
        if (arg is FalseNode)
            return TrueNode.Instance;
        return node;
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var solutions = context.Solver.Solve(new Query(arguments.Single()), scope);
        if (solutions.Any())
        {
            yield return False();
        }
        else
        {
            yield return True();
        }
    }
}
