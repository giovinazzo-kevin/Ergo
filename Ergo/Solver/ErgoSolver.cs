using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;
using System.ComponentModel;

namespace Ergo.Solver;

public partial class ErgoSolver : IDisposable
{
    public readonly SolverFlags Flags;
    public readonly Dictionary<Signature, SolverBuiltIn> BuiltIns;

    public readonly ErgoFacade Facade;
    public readonly KnowledgeBase KnowledgeBase;
    public readonly HashSet<Atom> DataSinks = new();
    public readonly Dictionary<Signature, HashSet<DataSource>> DataSources = new();

    public event Action<SolverTraceType, string> Trace;
    public event Action<ErgoSolver, ITerm> DataPushed;
    public event Action<ErgoSolver> Disposing;

    internal ErgoSolver(ErgoFacade facade, KnowledgeBase kb, SolverFlags flags = SolverFlags.Default)
    {
        Facade = facade;
        Flags = flags;
        KnowledgeBase = kb;
        BuiltIns = new();
    }

    public static Signature GetDataSignature<T>(Maybe<Atom> functor = default)
        where T : new()
    {
        var term = TermMarshall.ToTerm(new T());
        var signature = term.GetSignature();
        signature = signature.Tag.HasValue && WellKnown.Functors.Dict.Contains(signature.Functor)
            ? functor.Reduce(some => signature.WithTag(Maybe.Some(some)), () => signature)
            : functor.Reduce(some => signature.WithFunctor(some), () => signature);
        return signature;
    }

    public void BindDataSource<T>(DataSource<T> data)
        where T : new()
    {
        var signature = GetDataSignature<T>(Maybe.Some(data.Functor)).WithModule(Maybe.None<Atom>());
        if (!DataSources.TryGetValue(signature, out var hashSet))
        {
            DataSources[signature] = hashSet = new();
        }

        hashSet.Add(data.Source);
    }

    public void BindDataSink<T>(DataSink<T> sink)
        where T : new()
    {
        DataSinks.Add(sink.Functor);
        sink.Connect(this);
        Disposing += _ =>
        {
            sink.Disconnect(this);
            DataSinks.Remove(sink.Functor);
        };
    }

    public bool RemoveDataSources<T>(Atom functor)
        where T : new()
    {
        var signature = GetDataSignature<T>(Maybe.Some(functor));
        if (DataSources.TryGetValue(signature, out var hashSet))
        {
            hashSet.Clear();
            DataSources.Remove(signature);
            return true;
        }

        return false;
    }

    public void PushData(ITerm data) => DataPushed?.Invoke(this, data);

    public bool TryAddBuiltIn(SolverBuiltIn b) => BuiltIns.TryAdd(b.Signature, b);

    public async IAsyncEnumerable<KBMatch> GetDataSourceMatches(ITerm head)
    {
        // Allow enumerating all data sources by binding to a variable
        if (head is Variable)
        {
            foreach (var sig in DataSources.Keys)
            {
                var anon = sig.Arity.Reduce(some => sig.Functor.BuildAnonymousTerm(some), () => new Dict(sig.Tag.GetOrThrow()).CanonicalForm);
                if (!head.Unify(anon).TryGetValue(out var subs))
                {
                    continue;
                }

                await foreach (var item in GetDataSourceMatches(anon.Substitute(subs)))
                {
                    yield return item;
                }
            }

            yield break;
        }

        var signature = head.GetSignature();
        // Return results from data sources 
        if (DataSources.TryGetValue(signature.WithModule(Maybe.None<Atom>()), out var sources))
        {
            signature = DataSources.Keys.Single(k => k.Equals(signature));
            foreach (var source in sources)
            {
                await foreach (var item in source)
                {
                    var predicate = new Predicate(
                        "data source",
                        WellKnown.Modules.CSharp,
                        item.WithFunctor(signature.Tag.Reduce(some => some, () => signature.Functor)),
                        NTuple.Empty,
                        dynamic: true,
                        exported: false
                    );
                    if (predicate.Unify(head).TryGetValue(out var matchSubs))
                    {
                        predicate = Predicate.Substitute(predicate, matchSubs);
                        yield return new KBMatch(head, predicate, matchSubs);
                    }
                    else if (source.Reject(item))
                    {
                        break;
                    }
                }
            }
        }
    }

    public async IAsyncEnumerable<Evaluation> ResolveGoal(ITerm qt, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var any = false;
        var sig = qt.GetSignature();
        if (!qt.TryGetQualification(out var qm, out var term))
        {
            // Try resolving the built-in's module automatically
            foreach (var key in BuiltIns.Keys)
            {
                if (!scope.InterpreterScope.IsModuleVisible(key.Module.GetOrDefault()))
                    continue;
                var withoutModule = key.WithModule(default);
                if (withoutModule.Equals(sig) || withoutModule.Equals(sig.WithArity(Maybe<int>.None)))
                {
                    term.TryQualify(key.Module.GetOrDefault(), out qt);
                    sig = key;
                    break;
                }
            }
        }

        while (BuiltIns.TryGetValue(sig, out var builtIn)
        || BuiltIns.TryGetValue(sig = sig.WithArity(Maybe<int>.None), out builtIn))
        {
            if (ct.IsCancellationRequested)
                yield break;
            if (!qt.TryGetQualification(out _, out var qv))
                qv = qt;
            var args = qv.Reduce(
                a => Array.Empty<ITerm>(),
                v => Array.Empty<ITerm>(),
                c => c.Arguments
            );
            LogTrace(SolverTraceType.BuiltInResolution, $"{qt.Explain()}", scope.Depth);
            await foreach (var eval in builtIn.Apply(this, scope, args))
            {
                if (ct.IsCancellationRequested)
                    yield break;
                qt = eval.Result;
                sig = qt.GetSignature();
                await foreach (var inner in ResolveGoal(eval.Result, scope, ct))
                {
                    yield return new(inner.Result, inner.Substitutions.Concat(eval.Substitutions).Distinct().ToArray());
                }

                any = true;
            }
        }

        if (!any)
            yield return new(qt);
    }

    public void LogTrace(SolverTraceType type, ITerm term, int depth = 0) => LogTrace(type, term.Explain(), depth);
    public void LogTrace(SolverTraceType type, string s, int depth = 0) => Trace?.Invoke(type, $"{type.GetAttribute<DescriptionAttribute>().Description}: ({depth:00}) {s}");

    public async IAsyncEnumerable<ITerm> ExpandTerm(ITerm term, SolverScope scope = default, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var any = false;
        await foreach (var exp in Inner(term, scope, ct))
        {
            any = true;
            yield return exp;
        }

        if (any)
            yield break;

        // If this is a complex term, expand all of its arguments recursively and produce a combination of all solutions
        if (term is Complex cplx)
        {
            var expansions = new List<ITerm>[cplx.Arity];
            for (var i = 0; i < cplx.Arity; i++)
            {
                expansions[i] = new();
                await foreach (var argExp in ExpandTerm(cplx.Arguments[i], scope, ct))
                    expansions[i].Add(argExp);
            }

            var cartesian = expansions.CartesianProduct();
            foreach (var argList in cartesian)
            {
                any = true;
                // TODO: This might mess with abstract forms!
                yield return cplx.WithArguments(argList.ToArray());
            }

        }

        if (any)
            yield break;

        yield return term;

        async IAsyncEnumerable<ITerm> Inner(ITerm term, SolverScope scope = default, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested || term is Variable)
                yield break;
            var sig = term.GetSignature();
            // Try all modules in import order
            var modules = scope.InterpreterScope.GetLoadedModules();
            foreach (var mod in modules.Reverse())
            {
                if (!mod.Expansions.TryGetValue(sig, out var expansions))
                    continue;
                scope = scope.WithModule(mod.Name);
                foreach (var exp in expansions)
                {
                    // Expansions are defined as a 1ary lambda over a predicate definition.
                    // The head or body of the predicate MUST reference the lambda variable. (this is already checked by the directive)
                    // The head of the predicate is unified with the current term, then the body is solved.
                    // The lambda argument is unified with the outcome of the expansion and yielded.

                    // [Output] >> (head :- body(Output)).
                    if (!exp.Predicate.Head.Unify(term).TryGetValue(out var subs))
                        continue;

                    var pred = Predicate.Substitute(exp.Predicate, subs).Qualified();
                    LogTrace(SolverTraceType.Expansion, $"{pred.Head.Explain()}", scope.Depth);
                    await foreach (var sol in Solve(new Query(pred.Body), scope, ct))
                    {
                        if (ct.IsCancellationRequested)
                            yield break;

                        if (!sol.Simplify().Links.Value.TryGetValue(exp.OutputVariable, out var expanded))
                            yield return expanded = WellKnown.Literals.Discard;
                        else yield return expanded;

                        if (sol.Scope.IsCutRequested)
                            break;
                    }
                }
            }
        }

    }

    public (ITerm Qualified, IEnumerable<KBMatch> Matches) QualifyGoal(SolverScope scope, ITerm goal)
    {
        var matches = KnowledgeBase.GetMatches(goal, desugar: false);
        if (matches.Any())
        {
            return (goal, matches);
        }

        var isDynamic = false;
        if (!goal.IsQualified)
        {
            if (goal.TryQualify(scope.Module, out var qualified)
                && ((isDynamic |= scope.InterpreterScope.Modules[scope.Module].DynamicPredicates.Contains(qualified.GetSignature())) || true))
            {
                matches = KnowledgeBase.GetMatches(qualified, desugar: false);
                if (matches.Any())
                {
                    return (qualified, matches);
                }
            }

            if (scope.Callers.Length > 0 && scope.Callers.First() is { } clause)
            {
                if (goal.TryQualify(clause.DeclaringModule, out qualified)
                    && ((isDynamic |= scope.InterpreterScope.Modules[clause.DeclaringModule].DynamicPredicates.Contains(qualified.GetSignature())) || true))
                {
                    matches = KnowledgeBase.GetMatches(qualified, desugar: false);
                    if (matches.Any())
                    {
                        return (qualified, matches);
                    }
                }
            }
        }

        var signature = goal.GetSignature();
        var dynModule = signature.Module.Reduce(some => some, () => scope.Module);
        if (!KnowledgeBase.TryGet(signature, out var predicates) && !(isDynamic |= scope.InterpreterScope.Modules.TryGetValue(dynModule, out var m) && m.DynamicPredicates.Contains(signature)))
        {
            if (Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
            {
                scope.Throw(SolverError.UndefinedPredicate, signature.Explain());
                return (goal, Enumerable.Empty<KBMatch>());
            }
        }

        return (goal, Enumerable.Empty<KBMatch>());
    }

    public SolverScope CreateScope(InterpreterScope interpreterScope)
        => new(interpreterScope, 0, interpreterScope.Module, default, ImmutableArray<Predicate>.Empty, cut: null);

    public IAsyncEnumerable<Solution> Solve(Query goal, SolverScope scope, CancellationToken ct = default)
        => new SolverContext(this, scope).Solve(goal, ct: ct);

    public void Dispose()
    {
        Disposing?.Invoke(this);
        GC.SuppressFinalize(this);
    }
}
