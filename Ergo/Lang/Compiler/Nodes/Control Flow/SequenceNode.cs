using Ergo.Solver;

namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a logical conjunction.
/// </summary>
public class SequenceNode : ExecutionNode
{
    public SequenceNode(List<ExecutionNode> nodes) => Nodes = nodes;

    public List<ExecutionNode> Nodes { get; }
    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        return ExecuteSequence(ctx, solverScope, execScope, 0);
    }
    private IEnumerable<ExecutionScope> ExecuteSequence(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope, int index)
    {
        if (index >= Nodes.Count)
        {
            yield return execScope;
            yield break;
        }

        var currentNode = Nodes[index];
        foreach (var newScope in currentNode.Execute(ctx, solverScope, execScope))
        {
            // Run the remaining nodes in the sequence on the current scope
            foreach (var resultScope in ExecuteSequence(ctx, solverScope, newScope, index + 1))
            {
                yield return resultScope;

                if (resultScope.IsCut) // Stop the loop if a cut has been encountered
                {
                    yield break;
                }
            }
            if (newScope.IsCut)
            {
                yield return newScope;
                yield break;
            }
        }
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new SequenceNode(Nodes.Select(n => n.Instantiate(ctx, vars)).ToList());
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new SequenceNode(Nodes.Select(n => n.Substitute(s)).ToList());
    }
}
