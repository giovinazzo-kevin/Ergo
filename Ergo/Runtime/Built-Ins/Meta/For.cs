using PeterO.Numbers;
using System.Collections;

namespace Ergo.Runtime.BuiltIns;

public sealed class For : BuiltIn
{
    public For()
        : base("", new("for"), 4, WellKnown.Modules.Meta)
    {
    }

    class ListEnumerable(int from, int step, int count, bool discarded, SubstitutionMap env, Variable var) : IReadOnlyList<Solution>
    {
        Solution Get(int i)
        {
            if (discarded)
            {
                return new Solution(env);
            }
            var clone = env.Clone();
            clone.Add(new(var, new Atom(EDecimal.FromInt32(step * i + from))));
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


    public override ErgoVM.Op Compile() => vm =>
    {
        Inner()(vm);
        ErgoVM.Op Inner()
        {
            var args = vm.Args;
            if (args[1] is not Atom { Value: EDecimal from })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[1].Explain(false));
            if (args[2] is not Atom { Value: EDecimal to })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[2].Explain(false));
            if (args[3] is not Atom { Value: EDecimal step })
                return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[3].Explain(false));
            var (iFrom, iTo, iStep) = (from.ToInt32Checked(), to.ToInt32Checked(), step.ToInt32Checked());
            if (args[0] is not Variable { } var)
            {
                if (args[0] is not Atom { Value: EDecimal d })
                    return ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[0].Explain(false));
                var i_ = d.ToInt32Checked();
                if (i_ < iFrom || i_ >= iTo)
                    return ErgoVM.Ops.Fail;
                return ErgoVM.Ops.NoOp;
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
                var env = vm.CloneEnvironment();
                // In this case we can generate the solutions lazily since there's no continuation.
                // This allows us to return crazy amounts of solutions in O(1) time and memory,
                // with the catch that they're computed later when accessed.
                var n = (int)Math.Ceiling(count / (float)iStep);
                var enumerable = new ListEnumerable(iFrom, iStep, n, discarded, env, var);
                vm.Solution(_ => enumerable, n);
                // Signal the VM that we can break out of the current And (if any) and return these solutions.
                vm.Ready();
            }
            void ChooseBacktrack(ErgoVM vm)
            {
                if (vm.@continue == ErgoVM.Ops.NoOp)
                    BacktrackUnrolled(vm);
                else Backtrack(vm);
            }
        }
    };
}
