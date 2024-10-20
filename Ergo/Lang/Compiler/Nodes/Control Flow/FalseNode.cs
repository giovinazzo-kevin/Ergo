namespace Ergo.Lang.Compiler;

public class FalseNode : StaticNode
{
    public static readonly FalseNode Instance = new();
    static void Fail(ErgoVM vm) => vm.Fail();
    public override Op Compile() => Fail;
    public override string Explain(bool canonical = false) => "⊥";
}
