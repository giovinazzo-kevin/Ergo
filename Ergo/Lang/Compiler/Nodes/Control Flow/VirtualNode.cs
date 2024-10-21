namespace Ergo.Lang.Compiler;

public class VirtualNode(Op op, ImmutableArray<ITerm> args) : StaticNode
{
    public override Op Compile() => vm =>
    {
        OldBuiltInNode.SetArgs(args)(vm);
        op(vm);
    };
    public override string Explain(bool canonical = false) => "(virtual)";
}
