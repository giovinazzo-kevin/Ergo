﻿namespace Ergo.Lang.Compiler;
public class CyclicalCallNode : DynamicNode
{
    public class NodeRef(ExecutionNode node)
    {
        public ExecutionNode Node { get; set; } = node;
    }

    public readonly Signature Signature;
    public Predicate Clause { get; set; }
    public NodeRef Ref { get; set; } = new(default);
    public bool IsTailCall => Clause.Body != null && Predicate.IsTailCall(Goal, Clause.Body);
    public readonly ITerm Head;
    public override bool IsDeterminate => false;

    public CyclicalCallNode(ITerm goal) : base(goal)
    {
        Signature = goal.GetSignature();
        goal.GetQualification(out Head);
    }
    public override ErgoVM.Op Compile() => vm =>
    {
        var head = vm.Memory.StoreTerm(Goal);
        if (IsTailCall)
        {
            vm.Memory.StripQualification(head, out var headNoModule);
            var tail = headNoModule.Deref(vm.Memory);
            vm.SetArgs2(headNoModule.GetArgs(vm.Memory));
        }
        ErgoVM.Ops.Goal(head)(vm);
    };

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
