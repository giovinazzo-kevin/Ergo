using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Compiler;
using Ergo.Lang.Extensions;
using Ergo.Runtime;

namespace Tests;

public class TermMemoryTests(ErgoTestFixture fixture) : ErgoTests(fixture)
{

    private readonly object _lock = new();

    #region Rows
    [Theory]
    [InlineData("false", 0)]
    [InlineData("true", 0)]
    [InlineData("1", 0)]
    [InlineData("cool", 0)]
    #endregion
    public void ShouldStoreAtom(string parse, uint address)
        => ShouldStore<Atom>(parse, typeof(AtomAddress), address);
    #region Rows
    [Theory]
    [InlineData("_", 0)]
    [InlineData("ABCDE1", 0)]
    [InlineData("_X", 0)]
    #endregion
    public void ShouldStoreVariable(string parse, uint address)
        => ShouldStore<Variable>(parse, typeof(VariableAddress), address);
    #region Rows
    [Theory]
    [InlineData("f(a,b,c)", 0)]
    [InlineData("f(A,B,C)", 0)]
    [InlineData("f(g(a,b),h(C))", 2)]
    #endregion
    public void ShouldStoreStructure(string parse, uint address)
        => ShouldStore<Complex>(parse, typeof(StructureAddress), address);
    #region Rows
    [Theory]
    [InlineData("[1,2,3,4,5]", 0)]
    [InlineData("[1,2,3|[4,5]]", 0)]
    [InlineData("[1,2,3|R]", 0)]
    [InlineData("[f(a,b,c),[1,2],3|R]", 0)]
    #endregion
    public void ShouldStoreAbstract_List(string parse, uint address)
        => ShouldStore<List>(parse, typeof(AbstractAddress), address);
    #region Rows
    [Theory]
    [InlineData("pred.", 0)]
    [InlineData("pred :- false.", 0)]
    [InlineData("pred :- true.", 0)]
    [InlineData("pred(_A, _B) :- _B := _A + 1.", 0)]
    #endregion
    public void ShouldStorePredicate(string parse, uint address)
    {
        var parsed = InterpreterScope.Parse<Predicate>(parse)
            .GetOrThrow(new InvalidOperationException());
        var ckb = new CompiledKnowledgeBase(InterpreterScope, Memory, KnowledgeBase.DependencyGraph);
        lock (_lock)
        {
            Memory.Clear();
            var addr = ckb.CompileAndAssertA(parsed);
            Assert.Equal(typeof(PredicateAddress), addr.GetType());
            Assert.Equal(address, addr.Index);
        }
    }

    [Theory]
    [InlineData("pred(_A, _B) :- _B := _A + 1.", "':'(user, pred(1,1))", "':'(user, pred(1,1))", 1, 0)] // Should *match* but then fail, this part is not tested here
    [InlineData("pred(1, 5).", "':'(user, pred(A,B))", "':'(user, pred(1,5))", 1, 1)]
    [InlineData("pred :- true ; true.", "':'(user, pred)", "':'(user, pred)", 1, 2)]
    [InlineData("list:pred.", "pred", "':'(list, pred)", 0, 0)]
    [InlineData("list:pred.", "':'(list, pred)", "':'(list, pred)", 1, 1)]
    [InlineData("list:pred(4, 5).", "':'(list, pred(A, B))", "':'(list, pred(4, 5))", 1, 1)]
    [InlineData("pred.", "wrong_pred", "pred", 0, 0)]
    public void ShouldMatchPredicate(string parse, string head, string derefHead, int expectedMatches, int expectedSolutions)
    {
        var parsed = InterpreterScope.Parse<Predicate>(parse)
            .GetOrThrow(new InvalidOperationException());
        var parsedHead = InterpreterScope.Parse<ITerm>(head)
            .GetOrThrow(new InvalidOperationException());
        var parsedDerefHead = InterpreterScope.Parse<ITerm>(derefHead)
            .GetOrThrow(new InvalidOperationException());
        var vm = new ErgoVM(new CompiledKnowledgeBase(InterpreterScope, Memory, KnowledgeBase.DependencyGraph));
        lock (_lock)
        {
            vm.Memory.Clear();
            var predAddr = vm.CKB.CompileAndAssertA(parsed);
            var headAddr = vm.Memory.StoreTerm(parsedHead);
            var match = vm.CKB.GetMatches(headAddr);
            if (!match.TryGetValue(out var matches))
            {
                Assert.True(expectedMatches == 0);
                return;
            }
            var numSols = 0;
            var numMatches = 0;
            foreach (var m in matches)
            {
                Assert.Equal(parsedDerefHead.Explain(), m.Head.Deref(Memory).Explain());
                vm.Query = m.Body;
                vm.Run();
                numSols += vm.NumSolutions;
                numMatches++;
            }
            Assert.Equal((expectedSolutions, expectedMatches), (numSols, numMatches));
        }
    }

    [Fact]
    public void ShouldTailRecurse()
    {
        var test0 = InterpreterScope.Parse<ITerm>("':'(stdlib, tail_recurse(0))")
            .GetOrThrow(new InvalidOperationException());
        var test1 = InterpreterScope.Parse<ITerm>("':'(stdlib, tail_recurse(10))")
            .GetOrThrow(new InvalidOperationException());
        var test2 = InterpreterScope.Parse<ITerm>("':'(stdlib, tail_recurse(-10))")
            .GetOrThrow(new InvalidOperationException());
        var vm = new ErgoVM(new CompiledKnowledgeBase(InterpreterScope, new(), KnowledgeBase.DependencyGraph));
        lock (_lock)
        {
            var state = vm.Memory.SaveState();
            var test0Head = vm.Memory.StoreTerm(test0);
            vm.Query = ErgoVM.Ops.Goal(test0Head);
            vm.Run();
            Assert.Single(vm.Solutions);
            vm.Memory.LoadState(state);
            var test1Head = vm.Memory.StoreTerm(test1);
            vm.Query = ErgoVM.Ops.Goal(test1Head);
            vm.Run();
            Assert.Single(vm.Solutions);
            vm.Memory.LoadState(state);
            var test2Head = vm.Memory.StoreTerm(test2);
            vm.Query = ErgoVM.Ops.Goal(test2Head);
            vm.Run();
            Assert.Empty(vm.Solutions);
        }
    }

    protected void ShouldStore<T>(string parse, Type addressType, uint address)
        where T : ITerm
    {
        var parsed = InterpreterScope.Parse<T>(parse)
            .GetOrThrow(new InvalidOperationException());
        lock (_lock)
        {
            Memory.Clear();
            var addr = Memory.StoreTerm(parsed);
            Assert.Equal(addressType, addr.GetType());
            Assert.Equal(address, addr.Index);
        }
    }
}
