using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public abstract class ExecutionNode
{
    public abstract ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ExecutionNode Substitute(IEnumerable<Substitution> s);
    public abstract IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope);
}
