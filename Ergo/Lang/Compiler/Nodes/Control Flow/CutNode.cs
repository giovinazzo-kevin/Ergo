namespace Ergo.Lang.Compiler;

public class CutNode : StaticNode
{
    static void Cut(ErgoVM vm) => vm.Cut();
    public override Op Compile() => Cut;
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        var lastCut = nodes.LastOrDefault(x => x is CutNode);
        if (lastCut != null)
            nodes.RemoveAll(n => n is CutNode && n != lastCut);
        return nodes;
    }
    public override string Explain(bool canonical = false) => $"!";
}
