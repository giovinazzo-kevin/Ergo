using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public sealed class For : SolverBuiltIn
{
    private readonly Dictionary<int, ErgoVM.Op> cache = new();
    private readonly ErgoVM.Goal Compiled;
    public For()
        : base("", new("for"), 3, WellKnown.Modules.Meta)
    {
        Compiled = args =>
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
                    return ErgoVM.Ops.Fail;
                return ErgoVM.Ops.NoOp;
            }
            int i = iFrom;
            void Backtrack(ErgoVM vm)
            {
                if (!var.Ignored)
                {
                    var atom = new Atom(EDecimal.FromInt32(i));
                    vm.Environment.Add(new(var, atom));
                }
                if (++i <= iTo)
                {
                    vm.PushChoice(Backtrack);
                }
                else i = iFrom;
                vm.Solution();
            }
            return cache[hash] = Backtrack;
        };
    }

    class IntRef
    {
        public EDecimal Value;

        public static implicit operator EDecimal(IntRef r) => r.Value;
    }

    public override ErgoVM.Goal Compile() => Compiled;

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
    {
        yield break;
    }
}
