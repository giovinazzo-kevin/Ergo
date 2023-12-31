namespace Ergo.Lang.Compiler;

public class VirtualNode(ErgoVM.Op op) : StaticNode
{
    public override ErgoVM.Op Compile() => op;
    public override string Explain(bool canonical = false) => "(virtual)";
}
