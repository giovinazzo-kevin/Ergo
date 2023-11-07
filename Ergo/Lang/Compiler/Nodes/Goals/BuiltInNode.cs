using Ergo.Solver.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public SolverBuiltIn BuiltIn { get; }
    public BuiltInNode(DependencyGraphNode node, ITerm goal, SolverBuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
    }

    public override Action Compile(ErgoVM vm) => () =>
    {
        Goal.Substitute(vm.Environment).GetQualification(out var inst);

        var anySolutions = false;
        var goal = BuiltIn.Apply(vm.Context, vm.Scope, inst.GetArguments()).GetEnumerator();
        NextGoal();

        void NextGoal()
        {
            if (goal.MoveNext())
            {
                if (!goal.Current.Result)
                {
                    vm.Fail();
                    return;
                }
                anySolutions = true;
                vm.Solution(goal.Current.Substitutions);
            }
            else if (!anySolutions)
            {
                vm.Fail();
            }
        }
    };

    //public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    //{
    //    Goal.Substitute(execScope.CurrentSubstitutions).GetQualification(out var inst);
    //    foreach (var eval in BuiltIn.Apply(ctx, solverScope, inst.GetArguments()))
    //    {
    //        if (eval.Result)
    //            yield return execScope.ApplySubstitutions(eval.Substitutions).AsSolution().Now(this);
    //        else yield break;
    //    }
    //}
    public override ExecutionNode Optimize()
    {
        if (BuiltIn.Optimize(this).TryGetValue(out var optimized))
            return optimized;
        return this;
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new BuiltInNode(Node, Goal.Instantiate(ctx, vars), BuiltIn);
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new BuiltInNode(Node, Goal.Substitute(s), BuiltIn);
    }
}
