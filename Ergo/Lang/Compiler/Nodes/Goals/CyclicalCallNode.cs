

namespace Ergo.Lang.Compiler;
public class CyclicalCallNode : DynamicNode
{
    public readonly Signature Signature;
    public Predicate Clause { get; set; }
    public bool IsTailCall => Predicate.IsTailCall(Goal, Clause.Body);
    public CyclicalCallNode(ITerm goal) : base(goal)
    {
        Signature = goal.GetSignature();
    }
    private static RuntimeException StackEmpty = new(ErgoVM.ErrorType.StackEmpty);
    public override ErgoVM.Op Compile()
    {
        if (IsTailCall)
            return TailCallOptimization;
        return ErgoVM.Ops.Goal(Goal);

        void TailCallOptimization(ErgoVM vm)
        {
            var goal = ErgoVM.Ops.Goal(Goal);
            while (true)
            {
                goal(vm);
                if (vm.State == ErgoVM.VMState.Fail)
                    break;
                var cp = vm.PopChoice().GetOrThrow(StackEmpty);
                goal = cp.Continue;
                ErgoVM.Ops.SetEnvironment(cp.Environment)(vm);
            }
        }
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new CyclicalCallNode(Goal.Instantiate(ctx, vars)) { Clause = Clause };
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new CyclicalCallNode(Goal.Substitute(s)) { Clause = Clause };
    }
}
