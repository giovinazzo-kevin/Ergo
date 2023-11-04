using Ergo.Solver;

namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a goal that could not be resolved at compile time.
/// </summary>
public class DynamicNode : ExecutionNode
{
    public DynamicNode(ITerm goal)
    {
        Goal = goal;
    }

    public ITerm Goal { get; }

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        var inst = Goal.Substitute(execScope.CurrentSubstitutions);
        inst.GetQualification(out var ih);
        var callerRef = default(Maybe<PredicateCall>);
        foreach (var sol in ctx.Solve(new Query(inst), solverScope))
        {
            var ret = execScope
                .ApplySubstitutions(sol.Substitutions)
                .AsSolution()
                .ChoicePoint();
            yield return ret.Now(this);
            if (callerRef.TryGetValue(out _))
                continue;
            foreach (var caller in sol.Scope.Callers.Add(sol.Scope.Callee).Where(x => x.Context != null))
            {
                if (ih.Unify(caller.Predicate.Head).TryGetValue(out _))
                {
                    callerRef = caller;
                    break;
                }
            }
        }
        // TODO: Feels somewhat hackish, figure out a more elegant solution
        if (execScope.IsBranch && callerRef.Select(x => x.Context.IsCutRequested).GetOr(false))
        {
            yield return execScope.AsSolution(false).Cut();
        }
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new DynamicNode(Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new DynamicNode(Goal.Substitute(s));
    }

    public override string Explain(bool canonical = false) => $"{GetType().Name} ({Goal.Explain(canonical)})";
}
