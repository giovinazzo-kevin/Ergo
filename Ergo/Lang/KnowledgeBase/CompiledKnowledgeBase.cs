using Ergo.Interpreter;
using Ergo.Lang.Compiler;
using System.Diagnostics;

namespace Ergo.Lang;

using IX_Signature = (string Module, string Functor, int Arity);
using Match = TermMemory.PredicateCell;

public record class CompiledKnowledgeBase(InterpreterScope Scope, TermMemory Memory, DependencyGraph Graph)
{
    protected readonly SortedDictionary<IX_Signature, List<PredicateAddress>> _signatureIndex = [];
    protected readonly InstantiationContext IC = new("k");

    public void CompileGraph()
    {
        // Graph.Rebuild();
        foreach (var node in Graph.GetAllNodes())
        {
            if (node.Addresses.TryGetValue(out var addrList))
            {
                //foreach (var addr in addrList)
                //    yield return addr;
                continue;
            }
            addrList = [];
            foreach (var clause in node.Clauses)
            {
                addrList.Add(CompileAndAssertZ(clause));
                //yield return addrList[addrList.Count - 1];
            }
            node.Addresses = addrList;
        }
    }

    IX_Signature GetIX_Signature(PredicateAddress pk) => GetIX_Signature(Memory[pk].Head);
    IX_Signature GetIX_Signature(ITermAddress head)
    {
        var sig = head.GetSignature(Memory);
        return (
            sig.Module.Select(x => x.Value.ToString()).GetOr(null),
            sig.Functor.Value.ToString(),
            sig.Arity.GetOr(int.MaxValue)
        );
    }

    protected void Assert(PredicateAddress pk, bool endOfList = true)
    {
        var ix = GetIX_Signature(pk);
        if (!_signatureIndex.TryGetValue(ix, out var predicateList))
            _signatureIndex[ix] = predicateList = [];
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
    public int RetractAll(ITermAddress head) => RetractMatch(head, true).Count();
    public Maybe<List<PredicateAddress>> GetPredicates(IX_Signature ix)
        => (_signatureIndex.TryGetValue(ix, out var predicateList), predicateList);
    public Maybe<List<PredicateAddress>> GetPredicates(Signature signature)
        => (_signatureIndex.TryGetValue((signature.Module.Select(x => x.Value.ToString()).GetOr(null),
            signature.Functor.Value.ToString(),
            signature.Arity.GetOr(int.MaxValue)),
            out var predicateList), predicateList);

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
                if (cell.Head is null)
                    throw new InvalidOperationException();
                //StripQualification(cell.Head, out var cellHead);
                Debug.WriteLine($"MATCHING :: {head.Deref(Memory).Explain()} with {cell.Head.Deref(Memory).Explain()}");
                if (Memory.Unify(head, cell.Head, transaction: false))
                {
                    Debug.WriteLine($"         :: SUCCESS");
                    yield return cell;
                }
                else
                {
                    Debug.WriteLine($"         :: FAILURE");
                }
                Memory.LoadState(state);
                i++;
            }
        }

        bool StripQualification(ITermAddress term, out ITermAddress head)
        {
            head = term;
            if (term is StructureAddress a)
            {
                var functor = (AtomAddress)Memory[a][0];
                if (WellKnown.Functors.Module.Contains(Memory[functor]))
                {
                    head = Memory[a][2];
                    return true;
                }
            }
            return false;
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
        var module = p.Head.GetQualification(out _).GetOr(p.DeclaringModule);
        var head = inst.Head.Qualified(module);
        var headAddr = Memory.StoreTerm(head);
        var body = Scope.ExceptionHandler.TryGet(() => inst.ExecutionGraph.GetOrLazy(() => inst
                .ToExecutionGraph(Graph)
                .Optimized(flags))
            .Compile())
            .GetOr(ErgoVM.Ops.Fail);
        var args = headAddr.GetArgs(Memory);
        var tailArgs = args;
        if (p.IsTailRecursive)
        {
            inst.Body.Contents.Last().GetQualification(out var tail);
            var tailAddr = Memory.StoreTerm(tail);
            tailArgs = tailAddr.GetArgs(Memory);
        }
        var addr = Memory.StorePredicate(headAddr, PredicateCall, p.IsTailRecursive, p.IsDynamic);
        return addr;
        void PredicateCall(ErgoVM vm)
        {
            if (vm.Flag(VMFlags.TCO))
                vm.SetArgs2(tailArgs);
            else
                vm.SetArgs2(args);
            body(vm);
            vm.SuccessToSolution();
        }
    }
}

