namespace Ergo.Solver.BuiltIns;

public sealed class AnonymousComplex : SolverBuiltIn
{
    // TODO: Remove once Reflection module is up and running
    public AnonymousComplex()
        : base("", new("anon"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (!args[1].Matches<int>(out var arity))
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1].Explain());
            yield break;
        }

        if (args[0] is not Atom functor)
        {
            if (args[0].GetQualification(out var qs).TryGetValue(out var qm) && qs is Atom functor_)
            {
                var cplx = functor_.BuildAnonymousTerm(arity);
                yield return new(cplx.Qualified(qm));
                yield break;
            }

            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Functor, args[0].Explain());
            yield break;
        }

        yield return new(functor.BuildAnonymousTerm(arity));
    }
}
