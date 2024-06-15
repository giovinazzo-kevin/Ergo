using Ergo.Lang.Compiler;
using PeterO.Numbers;
using System.Collections;

namespace Ergo.Runtime.BuiltIns;

public sealed class For : BuiltIn
{
    public For()
        : base("", "for", 4, WellKnown.Modules.Meta)
    {
    }

    public override bool IsDeterminate(ImmutableArray<ITerm> args)
    {
        if (args[1] is not Atom { Value: EDecimal from })
            return false;
        if (args[2] is not Atom { Value: EDecimal to })
            return false;
        if (args[3] is not Atom { Value: EDecimal step })
            return false;
        var (iFrom, iTo, iStep) = (from.ToInt32Checked(), to.ToInt32Checked(), step.ToInt32Checked());
        var count = iTo - iFrom;
        var n = (int)Math.Ceiling(count / (float)iStep);
        return n <= 1;
    }

    class ForEnumerable(int from, int step, int count, bool discarded, TermMemory.State env, VariableAddress var, ErgoVM vm, ErgoVM.Op cnt) : ISolutionEnumerable
    {
        Solution Get(int i)
        {
            if (discarded && cnt == ErgoVM.Ops.NoOp)
                return new Solution(new(vm.Env));
            Atom k = EDecimal.FromInt32(step * i + from);
            ref var loopVar = ref vm.Memory[var];
            if (loopVar is AtomAddress constAddr)
                vm.Memory.Free(constAddr);
            loopVar = vm.Memory.StoreAtom(k);
            vm.Ready();
            cnt(vm);
            return new Solution(new(vm.Env));
        }
        public IEnumerator<Solution> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return Get(i);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count { get; } = count;
        public Solution this[int index] => Get(index);
    }


    public override ErgoVM.Op Compile() => vm =>
    {
        Inner()(vm);
        ErgoVM.Op Inner()
        {
            if (vm.Arg(1) is not Atom { Value: EDecimal from })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), vm.Arg(1).Explain(false));
            if (vm.Arg(2) is not Atom { Value: EDecimal to })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), vm.Arg(2).Explain(false));
            if (vm.Arg(3) is not Atom { Value: EDecimal step })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), vm.Arg(3).Explain(false));
            var (iFrom, iTo, iStep) = (from.ToInt32Checked(), to.ToInt32Checked(), step.ToInt32Checked());
            if (vm.Arg(0) is not Variable { } var)
            {
                if (vm.Arg(0) is not Atom { Value: EDecimal d })
                    return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), vm.Arg(0).Explain(false));
                var i_ = d.ToInt32Checked();
                if (i_ < iFrom || i_ >= iTo)
                    return ErgoVM.Ops.Fail;
                return ErgoVM.Ops.NoOp;
            }
            var varAddr = vm.Memory.StoreVariable(var.Name);
            var discarded = (var.Ignored && vm.IsSingletonVariable(var));
            int i = iFrom;
            var count = iTo - iFrom;
            var env = vm.Memory.SaveState();
            return ChooseBacktrack;
            void Backtrack(ErgoVM vm)
            {
                if (!discarded)
                {
                    vm.Memory.LoadState(env);
                    ref var loopVar = ref vm.Memory[varAddr];
                    if (loopVar is AtomAddress oldConst)
                        vm.Memory.Free(oldConst);
                    loopVar = vm.Memory.StoreAtom((Atom)EDecimal.FromInt32(i));
                }
                if ((i += iStep) < iTo)
                {
                    vm.PushChoice(Backtrack);
                }
                else
                {
                    i = iFrom;
                }
            }
            void BacktrackUnrolled(ErgoVM vm)
            {
                var cnt = vm.@continue;
                // We can generate the solutions lazily since the continuaiton will not create choice points.
                // We can return crazy amounts of solutions in O(1) time and memory, with the catch that
                // they're computed later when enumerated, spreading (and offloading) the computational cost.
                var n = (int)Math.Ceiling(count / (float)iStep);
                var varAddr = vm.Memory.StoreVariable(var.Name);
                var enumerable = new ForEnumerable(iFrom, iStep, n, discarded, env, varAddr, vm, cnt);
                vm.Solution(_ => enumerable, n);
                // Signal the VM that we can break out of the current And (if any) and return these solutions.
                vm.Ready();
            }
            void ChooseBacktrack(ErgoVM vm)
            {
                if (vm.Flag(VMFlags.ContinuationIsDet))
                    BacktrackUnrolled(vm);
                else Backtrack(vm);
            }
        }
    };
}
