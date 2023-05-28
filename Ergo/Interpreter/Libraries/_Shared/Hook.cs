using Ergo.Solver;

namespace Ergo.Interpreter.Libraries;

public readonly record struct Hook(Signature Signature)
{
    public bool IsDefined(SolverContext ctx) => ctx.Solver.KnowledgeBase.Get(Signature).TryGetValue(out _);

    public IEnumerable<Solution> Call(SolverContext ctx, SolverScope scope, ImmutableArray<ITerm> args, CancellationToken ct = default)
    {
        if (!IsDefined(ctx) && ctx.Solver.Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
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
        var module = Signature.Module.GetOr(WellKnown.Modules.User);
        anon = anon
            .Qualified(module);
        var callee = new Predicate($"<hook:{Signature.Explain()}>", module, anon, NTuple.Empty, dynamic: true, exported: false);
        foreach (var s in ctx.Solve(new(anon), scope.WithCallee(callee), ct: ct))
            yield return s;
    }
}