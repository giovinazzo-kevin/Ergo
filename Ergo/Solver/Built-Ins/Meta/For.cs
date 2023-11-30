using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public sealed class For : SolverBuiltIn
{
    private readonly Dictionary<int, ErgoVM.Op> cache = new();
    public For()
        : base("", new("for"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        var hash = args.GetHashCode();
        if (cache.TryGetValue(hash, out var op))
            return op;
        if (args[1] is not Atom { Value: EDecimal from })
            return cache[hash] = ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[1].Explain(false));
        if (args[2] is not Atom { Value: EDecimal to })
            return cache[hash] = ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[2].Explain(false));
        var (iFrom, iTo) = (from.ToInt32Checked(), to.ToInt32Checked());
        if (args[0] is not Variable { } var)
        {
            if (args[0] is not Atom { Value: EDecimal d })
                return cache[hash] = ErgoVM.Ops.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(EDecimal), args[0].Explain(false));
            var i_ = d.ToInt32Checked();
            if (i_ < iFrom || i_ >= iTo)
                return cache[hash] = ErgoVM.Ops.Fail;
            return cache[hash] = ErgoVM.Ops.NoOp;
        }
        int i = iFrom;
        void Backtrack(ErgoVM vm)
        {
            if (!var.Discarded)
            {
                vm.Environment.Add(new(var, new Atom(EDecimal.FromInt32(i))));
            }
            if (++i <= iTo)
            {
                vm.Solution();
                vm.PushChoice(Backtrack);
            }
            else
            {
                i = iFrom;
            }
        }
        void BacktrackUnrolled(ErgoVM vm)
        {
            i = iTo;
        loop:
            if (!var.Discarded)
            {
                vm.Environment.Add(new(var, new Atom(EDecimal.FromInt32(i))));
            }
            if (--i >= 0)
            {
                vm.Solution();
                goto loop;
            }
            else
            {
                i = iTo;
            }
        }
        void ChooseBacktrack(ErgoVM vm)
        {
            if (vm.@continue == ErgoVM.Ops.NoOp)
                BacktrackUnrolled(vm);
            else (cache[hash] = Backtrack)(vm);
        }
        return cache[hash] = ChooseBacktrack;
    };
}
