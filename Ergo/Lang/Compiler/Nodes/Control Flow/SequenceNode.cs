namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents a logical conjunction.
/// </summary>
public class SequenceNode : ExecutionNode
{
    public readonly bool IsRoot;

    public override bool IsGround => Nodes.All(n => n.IsGround);
    public override bool IsDeterminate => Nodes.All(n => n.IsDeterminate);

    public SequenceNode(List<ExecutionNode> nodes, bool isRoot = false)
    {
        Nodes = nodes;
        IsRoot = isRoot;
    }

    public List<ExecutionNode> Nodes { get; }
    public override int CheckSum => Nodes.Aggregate(0, (a, b) => HashCode.Combine(a, b.CheckSum));

    public SequenceNode AsRoot() => new(Nodes, true);

    public override ErgoVM.Op Compile() => ErgoVM.Ops.And(Nodes.Select(n => n.Compile()).ToArray());
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        var fixpoint = false;
        var newList = nodes.SelectMany(n =>
        {
            if (n is SequenceNode seq)
                return seq.Nodes.AsEnumerable();
            return new[] { n };
        }).ToList();
        // Remove duplicates such as consecutive truths or cuts.
        // Then, applies all available optimizations from the child nodes until a fixed point is reached.
        int checkSum, newCheckSum = 0;
        do
        {
            checkSum = newCheckSum;
            newCheckSum = newList.Aggregate(0, (a, b) => HashCode.Combine(a, b.CheckSum));
            for (int i = newList.Count - 1; i >= 0; i--)
            {
                var current = newList[i];
                if (i > 0)
                {
                    var lookbehind = newList[i - 1];
                    if (Redundant(lookbehind, current))
                        newList.RemoveAt(i - 1);
                    else if (Coalesce(lookbehind, current).TryGetValue(out var coalesced))
                    {
                        newList.RemoveAt(i);
                        newList.RemoveAt(--i);
                        newList.Insert(i, coalesced);
                    }
                }
            }
            for (int i = 0; i < newList.Count; i++)
            {
                var current = newList[i];
                if (current is FalseNode)
                {
                    if (newList.All(x => x.IsDeterminate))
                    {
                        newList.Clear();
                        newList.Add(current);
                        break;
                    }
                    // Remove everything after this false node
                    newList.RemoveRange(i + 1, newList.Count - i - 1);
                    break;
                }
                if (current is TrueNode)
                {
                    newList.RemoveRange(i, 1);
                }
            }
            if (IsRoot)
            {
                // These optimizations don't work on partial lists.
                while (newList.Count > 0 && RedundantStart(newList[0]))
                    newList.RemoveAt(0);
            }
            var optimizationPassesFromNodes = newList
                .Select(n => (n.OptimizationOrder, Optimize: (Func<List<ExecutionNode>, List<ExecutionNode>>)n.OptimizeSequence))
                    .OrderBy(x => x.OptimizationOrder)
                    .Select(n => n.Optimize);
            foreach (var opt in optimizationPassesFromNodes)
            {
                newList = opt(newList);
            }
            for (int i = 0; i < newList.Count; i++)
            {
                newList[i] = newList[i].Optimize();
            }
        }
        while (newCheckSum != checkSum);
        return newList;


        bool RedundantStart(ExecutionNode a)
        {
            return a is CutNode || a is TrueNode;
        }

        bool Redundant(ExecutionNode a, ExecutionNode b)
        {
            return (a is TrueNode && b is TrueNode)
                || (a is FalseNode && b is FalseNode)
                || (a is CutNode && b is CutNode)
                || (a is TrueNode && b is DynamicNode)
                || (a is TrueNode && b is SequenceNode)
                || (a is TrueNode && b is BranchNode);
        }
        Maybe<ExecutionNode> Coalesce(ExecutionNode a, ExecutionNode b)
        {
            if (a is TrueNode && b is FalseNode || a is FalseNode && b is TrueNode)
                return FalseNode.Instance;
            return default;
        }
    }
    public override void Analyze()
    {
        var cutIndex = -1;
        for (int i = Nodes.Count - 1; i >= 0; i--)
        {
            var node = Nodes[i];
            if (cutIndex < 0 && node is CutNode)
                cutIndex = i;
            var cut = i < cutIndex;
            node.Analyze();
            node.IsContinuationDet = true;
            if (!node.IsDeterminate && !cut)
                break;
        }
    }
    public override ExecutionNode Optimize()
    {
        var newList = OptimizeSequence(Nodes);
        if (newList.Count == 0)
            return TrueNode.Instance;
        if (newList.Count == 1)
            return newList[0];
        return new SequenceNode(newList, IsRoot);
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (IsGround) return this;
        return new SequenceNode(Nodes.Select(n => n.Instantiate(ctx, vars)).ToList(), IsRoot);
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new SequenceNode(Nodes.Select(n => n.Substitute(s)).ToList(), IsRoot);
    }
    public override string Explain(bool canonical = false) => Nodes.Select((n, i) => ((i == 0 ? "" : " ,") + n.Explain(canonical))).Join("\r\n");
}
