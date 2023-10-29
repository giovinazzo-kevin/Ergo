﻿using Ergo.Solver;

namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents an if-then-else statement.
/// </summary>
public class IfThenElseNode : ExecutionNode
{
    public IfThenElseNode(ExecutionNode condition, ExecutionNode trueBranch, ExecutionNode falseBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }
    public ExecutionNode FalseBranch { get; }


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
        else
        {
            foreach (var res in FalseBranch.Execute(ctx, solverScope, execScope))
            {
                yield return res;
            }
        }
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new IfThenElseNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars), FalseBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new IfThenElseNode(Condition.Substitute(s), TrueBranch.Substitute(s), FalseBranch.Substitute(s));
    }
}
