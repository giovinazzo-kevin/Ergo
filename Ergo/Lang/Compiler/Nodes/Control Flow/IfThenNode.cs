using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class IfThenNode : ExecutionNode
{
    public IfThenNode(ExecutionNode condition, ExecutionNode trueBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        var conditionSubs = new SubstitutionMap();
        var satisfied = false;
        foreach (var res in Condition.Execute(ctx, solverScope, execScope))
        {
            satisfied = true;
            conditionSubs.AddRange(res.CurrentSubstitutions);
        }
        if (satisfied)
        {
            execScope = execScope.ApplySubstitutions(conditionSubs);
            foreach (var res in TrueBranch.Execute(ctx, solverScope, execScope))
            {
                yield return res;
            }
        }
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new IfThenNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new IfThenNode(Condition.Substitute(s), TrueBranch.Substitute(s));
    }
    public override string Explain(bool canonical = false) => $"{Condition.Explain(canonical)}\r\n\t->({TrueBranch.Explain(canonical)})\r\n";
}
