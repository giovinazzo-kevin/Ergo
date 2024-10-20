namespace Ergo.Lang.Compiler;

public class TrueNode : StaticNode
{
    public static readonly TrueNode Instance = new();
    public override Op Compile() => Ops.NoOp;
    public override string Explain(bool canonical = false) => $"⊤";
}
