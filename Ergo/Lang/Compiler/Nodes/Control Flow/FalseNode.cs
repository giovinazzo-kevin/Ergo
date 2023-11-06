namespace Ergo.Lang.Compiler;

public class FalseNode : StaticNode
{
    public static readonly FalseNode Instance = new();
    public override Action Compile(ErgoVM vm) => vm.Fail;
    public override string Explain(bool canonical = false) => "⊥";
}
