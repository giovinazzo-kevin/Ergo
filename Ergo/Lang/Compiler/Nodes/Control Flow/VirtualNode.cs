using Ergo.Runtime.BuiltIns;

namespace Ergo.Lang.Compiler;

public class VirtualNode(ErgoVM.Op op, ImmutableArray<ITerm> args) : StaticNode
{
    public override ErgoVM.Op Compile() => vm =>
    {
        BuiltInNode.SetArgs(args)(vm);
        op(vm);
    };
    public override string Explain(bool canonical = false) => "(virtual)";
}
