namespace Ergo.Lang.Compiler;
public class CyclicalCallNode : DynamicNode
{
    public class NodeRef
    {
        private bool busy = false;
        public ExecutionNode Node { get; set; }
        public NodeRef(ExecutionNode node) => Node = node;
    }

    public readonly Signature Signature;
    public Predicate Clause { get; set; }
    public NodeRef Ref { get; set; } = new(default);
    public bool IsTailCall => Predicate.IsTailCall(Goal, Clause.Body);
    public readonly ITerm Head;
    public override bool IsDeterminate => false;

    public CyclicalCallNode(ITerm goal) : base(goal)
    {
        Signature = goal.GetSignature();
        goal.GetQualification(out Head);
    }
    public override ErgoVM.Op Compile()
    {
        if (IsTailCall)
        {
            return vm => ErgoVM.Ops.Goal(Goal)(vm);
        }
        return ErgoVM.Ops.Goal(Goal);
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new CyclicalCallNode(Goal.Instantiate(ctx, vars)) { Clause = Clause, Ref = Ref };
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new CyclicalCallNode(Goal.Substitute(s)) { Clause = Clause, Ref = Ref };
    }
}
