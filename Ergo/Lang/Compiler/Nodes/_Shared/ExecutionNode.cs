using Ergo.Solver;
using System.Diagnostics;

namespace Ergo.Lang.Compiler;

[DebuggerDisplay("{Explain(false)}")]
public abstract class ExecutionNode : IExplainable
{
    public virtual ExecutionNode Optimize() => this;
    public abstract ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    public abstract ExecutionNode Substitute(IEnumerable<Substitution> s);
    public abstract IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope);
    public abstract string Explain(bool canonical = false);
}
