using Ergo.Solver.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public SolverBuiltIn BuiltIn { get; }
    public BuiltInNode(DependencyGraphNode node, ITerm goal, SolverBuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
    }

    public override Action Compile(ErgoVM vm)
    {
        return () =>
        {
            Goal.Substitute(vm.Environment).GetQualification(out var inst);
            var args = inst.GetArguments();
            if (BuiltIn.Solve(vm, args))
            {
                return;
            }
            var goal = BuiltIn.Apply(vm.Context, vm.Scope, args).GetEnumerator();
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
                    vm.Solution(goal.Current.Substitutions);
                    vm.PushChoice(NextGoal);
                }
                else
                {
                    vm.Fail();
                }
            }
        };
    }

    public override ExecutionNode Optimize()
    {
        return BuiltIn.Optimize(this);
    }
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        return BuiltIn.OptimizeSequence(nodes);
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
