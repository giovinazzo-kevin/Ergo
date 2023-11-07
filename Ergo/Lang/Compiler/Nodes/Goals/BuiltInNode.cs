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
        var initialized = false;
        var goal = default(IEnumerator<Evaluation>);
        var self = ErgoVM.NoOp;
        self = () =>
        {
            if (!initialized)
            {
                Goal.Substitute(vm.Environment).GetQualification(out var inst);
                goal = BuiltIn.Apply(vm.Context, vm.Scope, inst.GetArguments()).GetEnumerator();
                initialized = true;
            }
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
                    vm.PushChoice(self);
                }
                else
                {
                    vm.Fail();
                    initialized = false;
                }
            }
        };
        return self;
    }

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
