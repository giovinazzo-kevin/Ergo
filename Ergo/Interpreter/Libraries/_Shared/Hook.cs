using Ergo.Solver;

namespace Ergo.Interpreter.Libraries;

public readonly record struct Hook(Signature Signature)
{
    public bool IsDefined(SolverContext ctx) => ctx.Solver.KnowledgeBase.Get(Signature).TryGetValue(out _);

    public async IAsyncEnumerable<Solution> Call(SolverContext ctx, SolverScope scope, ImmutableArray<ITerm> args, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!IsDefined(ctx))
        {
            scope.Throw(SolverError.UndefinedPredicate, Signature.Explain());
            yield break;
        }
        if (Signature.Arity.TryGetValue(out var arity) && args.Length != arity)
        {
            scope.Throw(SolverError.ExpectedNArgumentsGotM, arity, args.Length);
            yield break;
        }
        var anon = Signature.Functor
            .BuildAnonymousTerm(Signature.Arity.GetOr(0));
        if (anon is Complex cplx)
            anon = cplx.WithArguments(args);
        anon = anon
            .Qualified(Signature.Module.GetOr(WellKnown.Modules.User));
        await foreach (var s in ctx.SolveAsync(new(anon), scope, ct: ct))
            yield return s;
    }
}