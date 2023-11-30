using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Explain : SolverBuiltIn
{
    public Explain()
        : base("", new("explain"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        var expl = new Atom(args[0].AsQuoted(false).Explain(), false);
        return ErgoVM.Goals.Unify([args[1], expl]);
    };
}
