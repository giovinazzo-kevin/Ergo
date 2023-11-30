using Ergo.Lang.Compiler;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries;

public readonly record struct CompiledHook(Signature Signature, ExecutionGraph Graph, ITerm Head)
{
}

public readonly record struct Hook(Signature Signature)
{
    public bool IsDefined(KnowledgeBase kb, out IList<Predicate> predicates) => kb.Get(Signature).TryGetValue(out predicates);
    public Maybe<CompiledHook> Compile(KnowledgeBase kb)
    {
        if (kb.DependencyGraph is null || !IsDefined(kb, out var predicates))
            return default;
        var ctx = new InstantiationContext("__H");
        var unifyNode = kb.DependencyGraph.GetNode(WellKnown.Signatures.Unify).GetOrThrow(new InvalidOperationException());
        var unifyBuiltIn = new Unify();
        var anon = Signature.Functor
            .BuildAnonymousTerm(Signature.Arity.GetOr(0), ignoredVars: false);
        try
        {
            var root = predicates.Select(p =>
            {
                var s = p.Instantiate(ctx); s.Head.GetQualification(out var pHead);
                if (anon.Unify(pHead).TryGetValue(out var subs))
                {
                    var unifications = subs
                        .Select(s => ((ITerm)Unify.MakeComplex(s.Lhs, s.Rhs)).Qualified(WellKnown.Modules.Prologue));
                    s = s.WithHead(anon).WithBody(new(unifications.Concat(s.Body.Contents).ToImmutableArray()));
                }
                return s.ToExecutionGraph(kb.DependencyGraph).Root;
            }).Aggregate((a, b) => new BranchNode(a, b)).Optimize();
            var graph = new ExecutionGraph(anon, root);
            return new CompiledHook(Signature, graph, anon);
        }
        catch (CompilerException e) { }
        return default;
    }
    //public IEnumerable<Solution> Call(ErgoVM vm, ImmutableArray<ITerm> args, CancellationToken ct = default)
    //{
    //    if (!IsDefined(ctx) && ctx.Solver.Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
    //    {
    //        scope.Throw(SolverError.UndefinedPredicate, Signature.Explain());
    //        yield break;
    //    }
    //    if (Signature.Arity.TryGetValue(out var arity) && args.Length != arity)
    //    {
    //        scope.Throw(SolverError.ExpectedNArgumentsGotM, arity, args.Length);
    //        yield break;
    //    }
    //    var anon = Signature.Functor
    //        .BuildAnonymousTerm(Signature.Arity.GetOr(0));
    //    if (anon is Complex cplx)
    //        anon = cplx.WithArguments(args);
    //    var module = Signature.Module.GetOr(WellKnown.Modules.User);
    //    anon = anon.Qualified(module);
    //    var callee = new Predicate($"<hook:{Signature.Explain()}>", module, anon, NTuple.Empty, dynamic: true, exported: false, default);
    //    foreach (var s in ctx.Solve(new(anon), scope.WithCallee(new(callee, ctx)), ct: ct))
    //        yield return s;
    //}
}