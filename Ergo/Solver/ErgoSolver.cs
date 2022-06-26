using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;
using Ergo.Solver.DataBindings;
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

    public static Signature GetDataSignature<T>(Maybe<Atom> functor = default)
        where T : new()
    {
        var term = TermMarshall.ToTerm(new T());
        var signature = term.GetSignature();
        signature = signature.Tag.TryGetValue(out _) && WellKnown.Functors.Dict.Contains(signature.Functor)
            ? functor.Select(some => signature.WithTag(some)).GetOr(signature)
            : functor.Select(some => signature.WithFunctor(some)).GetOr(signature);
        return signature;
    }

    internal ErgoSolver(ErgoFacade facade, KnowledgeBase kb, SolverFlags flags = SolverFlags.Default)
    {
        Facade = facade;
        Flags = flags;
        KnowledgeBase = kb;
        BuiltIns = new();
    }
    public SolverScope CreateScope(InterpreterScope interpreterScope)
        => new(interpreterScope, 0, interpreterScope.Entry, default, ImmutableArray<Predicate>.Empty, cut: null);
    public void AddBuiltIn(SolverBuiltIn b)
    {
        if (!BuiltIns.TryAdd(b.Signature, b))
            throw new NotSupportedException("A builtin with the same signature was already added");
    }
    public void PushData(ITerm data) => DataPushed?.Invoke(this, data);

    public void BindDataSource<T>(DataSource<T> data)
        where T : new()
    {
        var signature = GetDataSignature<T>(data.Functor).WithModule(Maybe.None<Atom>());
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
        var signature = GetDataSignature<T>(functor);
        if (DataSources.TryGetValue(signature, out var hashSet))
        {
            hashSet.Clear();
            DataSources.Remove(signature);
            return true;
        }

        return false;
    }

    public async IAsyncEnumerable<KBMatch> GetDataSourceMatches(ITerm head)
    {
        // Allow enumerating all data sources by binding to a variable
        if (head is Variable)
        {
            foreach (var sig in DataSources.Keys)
            {
                var anon = sig.Arity
                    .Select(some => sig.Functor.BuildAnonymousTerm(some))
                    .GetOr(new Dict(sig.Tag.GetOr(WellKnown.Literals.Discard)).CanonicalForm);
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
                        item.WithFunctor(signature.Tag.GetOr(signature.Functor)),
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

    /// <summary>
    /// Attempts to resolve 'goal' as a built-in call, and evaluates its result. On failure evaluates 'goal' as-is.
    /// </summary>
    public async IAsyncEnumerable<Evaluation> ResolveGoal(ITerm goal, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var any = false;
        var sig = goal.GetSignature();
        if (!goal.IsQualified)
        {
            // Try resolving the built-in's module automatically
            foreach (var key in BuiltIns.Keys)
            {
                if (!key.Module.TryGetValue(out var module) || !scope.InterpreterScope.IsModuleVisible(module))
                    continue;
                var withoutModule = key.WithModule(default);
                if (withoutModule.Equals(sig) || withoutModule.Equals(sig.WithArity(Maybe<int>.None)))
                {
                    goal = goal.Qualified(module);
                    sig = key;
                    break;
                }
            }
        }

        while (BuiltIns.TryGetValue(sig, out var builtIn) || BuiltIns.TryGetValue(sig = sig.WithArity(Maybe<int>.None), out builtIn))
        {
            if (ct.IsCancellationRequested)
                yield break;
            goal.GetQualification(out goal);
            var args = goal.GetArguments();
            LogTrace(SolverTraceType.BuiltInResolution, $"{goal.Explain()}", scope.Depth);
            if (builtIn.Signature.Arity.TryGetValue(out var arity) && args.Length != arity)
            {
                scope.Throw(SolverError.UndefinedPredicate, sig.WithArity(args.Length).Explain());
                yield break;
            }

            await foreach (var eval in builtIn.Apply(this, scope, args))
            {
                if (ct.IsCancellationRequested)
                    yield break;
                goal = eval.Result;
                sig = goal.GetSignature();
                await foreach (var inner in ResolveGoal(eval.Result, scope, ct))
                {
                    yield return new(inner.Result, inner.Substitutions.Concat(eval.Substitutions).Distinct().ToArray());
                }

                any = true;
            }
        }

        if (!any)
            yield return new(goal);
    }

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
            var modules = scope.InterpreterScope.GetVisibleModules();
            foreach (var mod in modules.Reverse())
            {
                if (!mod.Expansions.TryGetValue(sig, out var expansions))
                    continue;
                scope = scope.WithModule(mod.Name);
                foreach (var exp in expansions)
                {
                    // Expansions are defined as a 1ary lambda over a predicate definition.
                    // The head or body of the predicate MUST reference the lambda variable. (this is already checked by the directive)
                    // The head of the predicate is unified with the current term, then the body is substituted and solved.
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
                            yield return WellKnown.Literals.Discard;
                        else yield return expanded;

                        if (sol.Scope.IsCutRequested)
                            break;
                    }
                }
            }
        }

    }

    /// <summary>
    /// Enumerates all implicit qualifications of 'goal' that are worth trying in the current scope.
    /// </summary>
    public static IEnumerable<ITerm> GetImplicitGoalQualifications(SolverScope scope, ITerm goal)
    {
        yield return goal;
        var isDynamic = false;
        if (!goal.IsQualified)
        {
            var qualified = goal.Qualified(scope.Module);
            if ((isDynamic |= scope.InterpreterScope.Modules[scope.Module].DynamicPredicates.Contains(qualified.GetSignature())) || true)
            {
                yield return qualified;
            }

            if (scope.Callers.Length > 0 && scope.Callers.First() is { } clause)
            {
                qualified = goal.Qualified(clause.DeclaringModule);
                if ((isDynamic |= scope.InterpreterScope.Modules[clause.DeclaringModule].DynamicPredicates.Contains(qualified.GetSignature())) || true)
                {
                    yield return qualified;
                }
            }
        }
    }
    public IAsyncEnumerable<Solution> Solve(Query goal, SolverScope scope, CancellationToken ct = default)
        => new SolverContext(this, scope).Solve(goal, ct: ct);

    public void LogTrace(SolverTraceType type, ITerm term, int depth = 0) => LogTrace(type, term.Explain(), depth);
    public void LogTrace(SolverTraceType type, string s, int depth = 0) => Trace?.Invoke(type, $"{type.GetAttribute<DescriptionAttribute>().Description}: ({depth:00}) {s}");

    public void Dispose()
    {
        Disposing?.Invoke(this);
        GC.SuppressFinalize(this);
    }
}
