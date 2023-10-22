namespace Ergo.Solver.BuiltIns;

public sealed class Explain : SolverBuiltIn
{
    public Explain()
        : base("", new("explain"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var expl = new Atom(arguments[0].AsQuoted(false).Explain(), false);
        if (!arguments[1].IsGround)
        {
            yield return True(new Substitution(arguments[1], expl));
        }
        else if (LanguageExtensions.Unify(arguments[1], expl).TryGetValue(out var subs))
        {
            yield return True(subs);
        }
        else
        {
            yield return False();
        }
    }
}
