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
        foreach (var node in Nodes)
        {
            var (any, cut) = (false, false);
            foreach (var res in node.Execute(ctx, solverScope, execScope))
            {
                any |= true; cut |= res.IsCut;
                yield return res;
            }
            if (!any)
                break;
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
