﻿using PeterO.Numbers;
using System.Collections;

namespace Ergo.Runtime.BuiltIns;

public sealed class For : ErgoBuiltIn
{
    public For()
        : base("", new("for"), 4, WellKnown.Modules.Meta)
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

    class ForEnumerable(int from, int step, int count, bool discarded, SubstitutionMap env, Variable var, ErgoVM vm, Op cnt) : ISolutionEnumerable
    {
        Solution Get(int i)
        {
            if (discarded && cnt == Ops.NoOp)
            {
                return new Solution(env);
            }
            var clone = env.Clone();
            vm.Environment = clone;
            clone.Add(new(var, new Atom(EDecimal.FromInt32(step * i + from))));
            vm.Ready();
            cnt(vm);
            return new Solution(clone);
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


    public override Op Compile() => vm =>
    {
        Inner()(vm);
        Op Inner()
        {
            var args = vm.Args;
            if (args[1] is not Atom { Value: EDecimal from })
                return Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[1].Explain(false));
            if (args[2] is not Atom { Value: EDecimal to })
                return Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[2].Explain(false));
            if (args[3] is not Atom { Value: EDecimal step })
                return Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[3].Explain(false));
            var (iFrom, iTo, iStep) = (from.ToInt32Checked(), to.ToInt32Checked(), step.ToInt32Checked());
            if (args[0] is not Variable { } var)
            {
                if (args[0] is not Atom { Value: EDecimal d })
                    return Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[0].Explain(false));
                var i_ = d.ToInt32Checked();
                if (i_ < iFrom || i_ >= iTo)
                    return Ops.Fail;
                return Ops.NoOp;
            }
            var discarded = (var.Ignored && vm.IsSingletonVariable(var));
            int i = iFrom;
            var count = iTo - iFrom;
            return ChooseBacktrack;
            void Backtrack(ErgoVM vm)
            {
                if (!discarded)
                {
                    vm.Environment.Add(new(var, new Atom(EDecimal.FromInt32(i))));
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
                var env = vm.CloneEnvironment();
                // We can generate the solutions lazily since the continuaiton will not create choice points.
                // We can return crazy amounts of solutions in O(1) time and memory, with the catch that
                // they're computed later when enumerated, spreading (and offloading) the computational cost.
                var n = (int)Math.Ceiling(count / (float)iStep);
                var enumerable = new ForEnumerable(iFrom, iStep, n, discarded, env, var, vm, cnt);
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
