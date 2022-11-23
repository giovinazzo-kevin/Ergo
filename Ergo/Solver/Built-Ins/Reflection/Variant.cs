namespace Ergo.Solver.BuiltIns;

public sealed class Variant : SolverBuiltIn
{
    public Variant()
        : base("", new("variant"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsVariantOf(args[1]))
            yield return True(new SubstitutionMap());
        else yield return False();
    }
}
