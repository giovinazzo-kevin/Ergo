using Ergo.Lang.Ast;
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
public record LazyNode(Func<Op> op) : ExecutionNode
{
    public override Op Compile() => op();
}
public abstract record CallNode(MemoryAddress goal, Func<Op> compile) : ExecutionNode
{
    public override Op Compile()
    {
        var op = compile();
        return vm =>
        {
            vm.SetArg(1, goal);
            op(vm);
        };
    }
}

public sealed record TrueNode() : ConstNode(Ops.NoOp);
public sealed record FalseNode() : ConstNode(Ops.Fail);
public sealed record CutNode() : ConstNode(Ops.Cut);

public sealed record DynamicCallNode(MemoryAddress Goal) : CallNode(Goal, () => Ops.Goal(null));
public sealed record StaticCallNode(MemoryAddress Goal, PredicateNode Predicate) : CallNode(Goal, Predicate.Compile);

public record IfThenNode(ExecutionNode @if, ExecutionNode then) 
    : LazyNode(() => Ops.IfThen(@if.Compile(), then.Compile()));
public record IfThenElseNode(ExecutionNode @if, ExecutionNode then, ExecutionNode @else)
    : LazyNode(() => Ops.IfThenElse(@if.Compile(), then.Compile(), @else.Compile()));
public record BranchNode(params ExecutionNode[] Nodes)
    : LazyNode(() => Ops.Or([.. Nodes.Select(x => x.Compile())]));
public record SequenceNode(params ExecutionNode[] Nodes)
    : LazyNode(() => Ops.And([.. Nodes.Select(x => x.Compile())]));

public abstract record ArgNode
{

}

public sealed record UnifyHeadNode(MemoryAddress Head) : ExecutionNode
{
    public override Op Compile() => vm =>
    {
        vm.SetArg(0, Head);
        Goals.Unify2(vm);
    };
}

public sealed record ClauseNode(MemoryAddress Head, CallNode[] Goals) : IfThenNode(new UnifyHeadNode(Head), new SequenceNode(Goals));
public sealed record BuiltInNode(MemoryAddress Head, ErgoBuiltIn BuiltIn) : IfThenNode(new UnifyHeadNode(Head), new LazyNode(BuiltIn.Compile));
public sealed record PredicateNode(int numClauses, Maybe<BuiltInNode> builtIn) : ExecutionNode
{
    public readonly ClauseNode[] Clauses = new ClauseNode[numClauses];
    public readonly Maybe<BuiltInNode> BuiltIn = builtIn;

    public override Op Compile() 
        => new BranchNode([.. BuiltIn.AsEnumerable(), .. Clauses]).Compile();
}


public class ErgoExecutionGraph
{
    private readonly Dictionary<PredicateDefinition, PredicateNode> Map = [];

    public PredicateNode this[PredicateDefinition def] => Map[def];

    public bool TryGet(PredicateDefinition def, out PredicateNode result) => Map.TryGetValue(def, out result);

    public void Declare(PredicateDefinition pred, int numClauses, Maybe<BuiltInNode> builtIn)
    {
        Map.Add(pred, new(numClauses, builtIn));
    }
}


public readonly record struct MemoryAddress(int Address)
{
    public static implicit operator int(MemoryAddress a) => a.Address;
    public static implicit operator MemoryAddress(int a) => new(a);
};
public abstract record MemoryCell;
public sealed record ConCell(object Value) : MemoryCell;
public sealed record RefCell : MemoryCell
{
    public MemoryAddress Address { get; set; }
}
public sealed record SigCell(object Functor, int Arity) : MemoryCell;

public class ErgoMemory
{
    private MemoryCell[] _heap = new MemoryCell[2048];
    private readonly Stack<(MemoryAddress Lhs, MemoryAddress Rhs)> _unificationStack = [];

    protected int HP { get; private set; }
    
    protected void Allocate(int blocks)
    {
        if (_heap.Length - HP > blocks)
            return;
        Array.Resize(ref _heap, _heap.Length * 2);
    }

    protected void Push(MemoryCell cell)
    {
        this[HP++] = cell;
    }

    public MemoryAddress Store(ArgDefinition arg, Dictionary<int, int> varMap = default)
    {
        var hp = HP;
        varMap ??= [];
        switch (arg)
        {
            case ConstArgDefinition { Value: var @const }:
                Push(new ConCell(@const));
                return hp;
            case VariableArgDefinition { VariableIndex: var @var }:
                if (varMap.TryGetValue(var, out var addr))
                    Push(new RefCell { Address = addr });
                else
                    Push(new RefCell { Address = varMap[var] = HP });
                return hp;
            case ComplexArgDefinition { Functor: var @const, Args: { } args }:
                Push(new SigCell(@const, args.Length));
                foreach (var cplxArg in args)
                    Store(cplxArg, varMap);
                return hp;
        }
        throw new NotSupportedException();
    }

    public MemoryAddress StoreHead(ClauseDefinition clause, Dictionary<int, int> varMap = default)
    {
        if (clause.Arity == 0)
            return Store(new ConstArgDefinition(clause.Functor.Value), varMap);
        return Store(new ComplexArgDefinition(clause.Functor.Value, clause.Args), varMap);
    }

    public MemoryAddress StoreHead(ErgoBuiltIn builtIn)
    {
        var arity = builtIn.Signature.Arity.GetOrThrow(); // TODO: reimplement variadics
        if (arity == 0)
            return Store(new ConstArgDefinition(builtIn.Signature.Functor.Value));
        var args = Enumerable.Range(0, arity)
            .Select(i => new VariableArgDefinition(0))
            .ToArray();
        return Store(new ComplexArgDefinition(builtIn.Signature.Functor.Value, args));
    }

    public MemoryCell Deref(ref MemoryAddress addr)
    {
        var cell = this[addr];
        if (cell is RefCell { Address: var refAddr } refCell)
        {
            if (refAddr.Address == addr.Address)
                return cell;
            addr = refAddr.Address;
            if ((cell = this[refAddr]) is not RefCell)
                return cell;
            var refChain = new List<RefCell>() { refCell };
            while (this[addr] is RefCell @ref)
            {
                if (refChain.Contains(@ref))
                    break;
                addr = @ref.Address;
                refChain.Add(@ref);
            }
            foreach (var @ref in refChain)
                @ref.Address = addr;
            return this[addr];
        }
        return cell;
    }

    public ITerm Materialize(MemoryAddress addr)
    {
        switch(this[addr])
        {
            case ConCell { Value: var val}:
                return new Atom(val);
            case RefCell { Address: var refAddr}:
                return new Variable($"__{refAddr}");
            case SigCell { Functor: var functor, Arity: var arity }:
                var args = Enumerable
                    .Range(addr + 1, arity)
                    .Select(a => Materialize(a))
                    .ToArray();
                return new Complex(new Atom(functor), args);
        }
        throw new NotSupportedException();
    }

    public bool Unify(MemoryAddress lhs, MemoryAddress rhs)
    {
        if (lhs.Address == rhs.Address)
            return true;
        _unificationStack.Push((lhs, rhs));
        while (_unificationStack.TryPop(out var equation))
        {
            (lhs, rhs) = (equation.Lhs, equation.Rhs);
            switch ((Deref(ref lhs), Deref(ref rhs)))
            {
                case (RefCell lref, RefCell rref):
                    var (laddr, raddr) = (lref.Address, rref.Address);
                    (lref.Address, rref.Address) = (raddr, laddr);
                    break;
                case (RefCell lref, _):
                    lref.Address = rhs;
                    break;
                case (_, RefCell rref):
                    rref.Address = lhs;
                    break;
                case (ConCell { Value: var lvalue }, ConCell { Value: var rvalue }):
                    if (!Equals(lvalue, rvalue))
                        return false;
                    break;
                case (SigCell { Functor: var lfun, Arity: var lar }, SigCell { Functor: var rfun, Arity: var rar }):
                    if (lar != rar || !Equals(lfun, rfun))
                        return false;
                    for (int i = 0; i < lar; i++)
                        _unificationStack.Push((lhs + i + 1, rhs + i + 1));
                    break;
                default:
                    return false;
            }
        }
        return true;
    }

    public MemoryCell this[int addr]
    {
        get => _heap[addr];
        set => _heap[addr] = value;
    }
}