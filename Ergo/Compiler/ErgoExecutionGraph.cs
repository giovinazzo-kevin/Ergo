using Ergo.Runtime.BuiltIns;
using static Ergo.Runtime.ErgoVM;

namespace Ergo.Compiler;

public abstract record ExecutionNode
{
    public abstract Op Compile();
}
public abstract record ConstNode(Op op) : ExecutionNode
{
    public override Op Compile() => op;
}
public abstract record CallNode(Func<Op> op) : ExecutionNode
{
    public override Op Compile() => op();
}

public sealed record TrueNode() : ConstNode(Ops.NoOp);
public sealed record FalseNode() : ConstNode(Ops.Fail);
public sealed record CutNode() : ConstNode(Ops.Cut);

public sealed record DynamicCallNode(Signature Goal) : CallNode(() => Ops.Goal(null));
public sealed record StaticCallNode(PredicateNode Goal) : CallNode(Goal.Compile);

public record IfThenNode(ExecutionNode @if, ExecutionNode then) 
    : CallNode(() => Ops.IfThen(@if.Compile(), then.Compile()));
public record IfThenElseNode(ExecutionNode @if, ExecutionNode then, ExecutionNode @else)
    : CallNode(() => Ops.IfThenElse(@if.Compile(), then.Compile(), @else.Compile()));
public record BranchNode(params ExecutionNode[] Nodes)
    : CallNode(() => Ops.Or([.. Nodes.Select(x => x.Compile())]));
public record SequenceNode(params ExecutionNode[] Nodes)
    : CallNode(() => Ops.And([.. Nodes.Select(x => x.Compile())]));

public enum TermType
{
    Atom,
    Variable,
    Complex
}
public enum RegisterType
{
    Args
}
public readonly record struct TermAddress(TermType Type, uint Addr);
public readonly record struct RegisterAddress(RegisterType Type, uint Addr);

public abstract record ArgNode
{
    public TermAddress Store()
    {

    }

    public void Load(TermAddress term)
}

public sealed record UnifyNode(TermAddress Lhs, TermAddress Rhs) : ExecutionNode
{
    public override Op Compile() => Goals.Unify2;
}
public sealed record ClauseNode(CallNode[] Goals) 
    : IfThenNode(null, new SequenceNode(Goals))
{

}
public sealed record PredicateNode(ClauseNode[] Clauses, Maybe<BuiltInNode> BuiltIn) : BranchNode([.. BuiltIn.AsEnumerable(), .. Clauses]);
public sealed record BuiltInNode(ErgoBuiltIn BuiltIn) : CallNode(BuiltIn.Compile);


public class ErgoExecutionGraph
{

    public bool TryGetPredicate(Signature sig, out PredicateNode node)
    {
        var module = sig.Module;
        node = null; return true;
    }

    public void Define(PredicateDefinition pred, PredicateNode node)
    {
    }
}
