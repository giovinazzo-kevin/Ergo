namespace Ergo.Lang.Compiler;

public class CutNode : StaticNode
{
    public override Action Compile(ErgoVM vm) => vm.Cut;
    public override string Explain(bool canonical = false) => $"!";
}
