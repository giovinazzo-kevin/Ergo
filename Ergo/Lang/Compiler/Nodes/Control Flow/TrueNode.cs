namespace Ergo.Lang.Compiler;

public class TrueNode : StaticNode
{
    public static readonly TrueNode Instance = new();
    public override Action Compile(ErgoVM vm) => () =>
    {
        vm.Solution();
    };
    public override string Explain(bool canonical = false) => $"⊤";
}
