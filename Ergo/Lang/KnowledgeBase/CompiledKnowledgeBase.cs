using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.Lang;

using IX_Signature = (string Functor, int Arity);
using Match = TermMemory.PredicateCell;

public record class CompiledKnowledgeBase(InterpreterScope Scope, TermMemory Memory, DependencyGraph Graph)
{
    protected readonly SortedDictionary<IX_Signature, List<PredicateAddress>> _signatureIndex = [];
    protected readonly InstantiationContext IC = new("k");

    public IEnumerable<PredicateAddress> CompileGraph()
    {
        foreach (var node in Graph.GetAllNodes())
        {
            if (node.Addresses.TryGetValue(out var addrList))
            {
                foreach (var addr in addrList)
                    yield return addr;
                continue;
            }
            addrList = [];
            foreach (var clause in node.Clauses)
            {
                addrList.Add(CompileAndAssertZ(clause));
                yield return addrList[addrList.Count - 1];
            }
            node.Addresses = addrList;
        }
    }

    IX_Signature GetIX_Signature(PredicateAddress pk) => GetIX_Signature(Memory[pk].Head);
    IX_Signature GetIX_Signature(ITermAddress head)
    {
        var sig = head.GetSignature(Memory);
        return (
            sig.Functor.Value.ToString(),
            sig.Arity.GetOr(int.MaxValue)
        );
    }

    protected void Assert(PredicateAddress pk, bool endOfList = true)
    {
        var ix = GetIX_Signature(pk);
        var key = (ix.Functor, ix.Arity);
        if (!_signatureIndex.TryGetValue(key, out var predicateList))
            _signatureIndex[key] = predicateList = [];
        if (endOfList)
            predicateList.Add(pk);
        else
            predicateList.Insert(0, pk);
    }
    public void AssertA(PredicateAddress pk) => Assert(pk, endOfList: false);
    public void AssertZ(PredicateAddress pk) => Assert(pk, endOfList: true);
    public bool Retract(IX_Signature ix)
    {
        if (GetPredicates(ix).TryGetValue(out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var cell = Memory[list[i]];
                if (!cell.IsDynamic)
                    continue;
                Memory.FreePredicate(list[i]);
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    public bool Retract(PredicateAddress addr)
    {
        if (GetPredicates(GetIX_Signature(addr)).TryGetValue(out var list))
            return list.Remove(addr);
        return false;
    }
    public int RetractAll(IEnumerable<PredicateAddress> addrs) => addrs
        .Select(Retract)
        .Count(x => x);
    public int RetractAll(IEnumerable<Match> cells) => cells
        .Select(x => x.Addr)
        .Select(Retract)
        .Count(x => x);
    public int RetractAll(IX_Signature ix)
    {
        if (GetPredicates(ix).TryGetValue(out var list))
            return list.RemoveAll(x => Memory[x].IsDynamic);
        return 0;
    }
    private IEnumerable<PredicateAddress> RetractMatch(ITermAddress head, bool all = false)
    {
        int i = 0;
        if (GetMatches(head).TryGetValue(out var enumerable))
        {
            foreach (var cell in enumerable)
            {
                if (Retract(cell.Addr))
                {
                    i++;
                    yield return cell.Addr;
                }
                if (!all)
                    break;
            }
        }
    }
    public Maybe<PredicateAddress> Retract(ITermAddress head) => Maybe.FromEnumerable(RetractMatch(head, false));
    public IEnumerable<PredicateAddress> RetractAll(ITermAddress head) => RetractMatch(head, true).ToArray();
    public Maybe<List<PredicateAddress>> GetPredicates(IX_Signature ix)
        => (_signatureIndex.TryGetValue(ix, out var predicateList), predicateList);

    public Maybe<IEnumerable<Match>> GetMatches(ITermAddress head)
    {
        var state = Memory.SaveState();
        var ix = GetIX_Signature(head);
        if (!GetPredicates(ix).TryGetValue(out var list))
            return default;
        return Maybe.Some(Inner());
        IEnumerable<Match> Inner()
        {
            int i = 0;
            foreach (var addr in list)
            {
                var cell = Memory[addr];
                if (Memory.Unify(head, cell.Head, transaction: false))
                    yield return cell;
                Memory.LoadState(state);
                i++;
            }
        }
    }

    public PredicateAddress CompileAndAssertA(Predicate p, OptimizationFlags flags = OptimizationFlags.Default)
    {
        var addr = CompileAndStorePredicate(p, flags);
        AssertA(addr);
        return addr;
    }
    public PredicateAddress CompileAndAssertZ(Predicate p, OptimizationFlags flags = OptimizationFlags.Default)
    {
        var addr = CompileAndStorePredicate(p, flags);
        AssertZ(addr);
        return addr;
    }

    protected PredicateAddress CompileAndStorePredicate(Predicate p, OptimizationFlags flags)
    {
        var inst = p.Instantiate(IC);
        var head = Memory.StoreTerm(inst.Head);
        var body = Scope.ExceptionHandler.TryGet(() => inst.ExecutionGraph.GetOrLazy(() => inst
                .ToExecutionGraph(Graph)
                .Optimized(flags))
            .Compile())
            .GetOr(ErgoVM.Ops.Fail);
        var args = head.GetArgs(Memory);
        var addr = Memory.StorePredicate(head, PredicateCall, p.IsDynamic);
        return addr;
        void PredicateCall(ErgoVM vm)
        {
            vm.SetArgs2(args);
            body(vm);
        }
    }
}

