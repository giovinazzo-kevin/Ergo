namespace Ergo.Solver.BuiltIns;

public sealed class AnonymousComplex : SolverBuiltIn
{
    public AnonymousComplex()
        : base("", new("anon"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
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
                var cplx = functor_.BuildAnonymousTerm(arity)
                    .Qualified(qm);
                if (LanguageExtensions.Unify(cplx, args[2]).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
                yield return False();
                yield break;
            }

            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Functor, args[0].Explain());
            yield break;
        }
        var anon = functor.BuildAnonymousTerm(arity)
            .Qualified(scope.InterpreterScope.Entry);
        if (LanguageExtensions.Unify(anon, args[2]).TryGetValue(out var subs_))
        {
            yield return True(subs_);
            yield break;
        }
        yield return False();
        yield break;
    }
}
