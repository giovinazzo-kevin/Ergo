using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class VariableNode : ExecutionNode
{
    public Variable Binding { get; private set; }

    public VariableNode(Variable v)
    {
        Binding = v;
    }

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        yield return execScope;
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new VariableNode((Variable)Binding.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        var term = ((ITerm)Binding).Substitute(s);
        if (term is not Variable)
            return new DynamicNode(term);
        return new VariableNode((Variable)term);
    }
}
