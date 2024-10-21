using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override bool IsDeterminate(ImmutableArray<ITerm> args) => true;
    public override int OptimizationOrder => base.OptimizationOrder + 20;
    public override ExecutionNode Optimize(OldBuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        if (!args[1].IsGround)
            return node;
        try
        {
            var ret = new Eval().Evaluate(null, args[1]);
            if (args[0].IsGround)
            {
                if (args[0].Equals(new Atom(ret)))
                    return TrueNode.Instance;
            }
            else if (node.DependencyGraph.Predicates.TryGetValue(WellKnown.Signatures.Unify, out var unifyNode))
            {
                return new OldBuiltInNode(node.DependencyGraph, unifyNode, Unify.MakeComplex(args[0], new Atom(ret)));
            }
            return FalseNode.Instance;
        }
        catch (RuntimeException)
        {
            return FalseNode.Instance;
        }
    }

    public override Op Compile() => vm =>
    {
        var arg = vm.Arg(1);
        var eval = new Atom(Evaluate(vm, arg));
        if (vm.State == ErgoVM.VMState.Fail)
            return;
        vm.SetArg(1, eval);
        ErgoVM.Goals.Unify2(vm);
    };
}
