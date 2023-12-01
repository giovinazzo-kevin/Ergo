using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override int OptimizationOrder => base.OptimizationOrder + 20;
    public override ExecutionNode Optimize(BuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        if (!args[1].IsGround)
            return node;
        var ret = new Eval().Evaluate(null, args[1]);
        if (args[0].IsGround)
        {
            if (args[0].Equals(new Atom(ret)))
                return TrueNode.Instance;
        }
        else if (node.Node.Graph.GetNode(WellKnown.Signatures.Unify).TryGetValue(out var unifyNode))
        {
            return new BuiltInNode(unifyNode, Unify.MakeComplex(args[0], new Atom(ret)), node.Node.Graph.UnifyInstance);
        }
        return FalseNode.Instance;
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var eval = new Atom(Evaluate(vm, args[1]));
        if (vm.State == ErgoVM.VMState.Fail)
            return;
        ErgoVM.Goals.Unify(args.SetItem(1, eval))(vm);
    };
}
