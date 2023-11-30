using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Unifiable : SolverBuiltIn
{
    public Unifiable()
        : base("", new("unifiable"), Maybe<int>.Some(3), WellKnown.Modules.Prologue)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        if (args[0].Unify(args[1]).TryGetValue(out var subs))
        {
            var equations = subs.Select(s => (ITerm)new Complex(WellKnown.Operators.Unification.CanonicalFunctor, s.Lhs, s.Rhs)
                .AsOperator(WellKnown.Operators.Unification));
            List list = new(ImmutableArray.CreateRange(equations), default, default);
            return ErgoVM.Goals.Unify([args[2], list]);
        }
        return ErgoVM.Ops.Fail;
    };
}
