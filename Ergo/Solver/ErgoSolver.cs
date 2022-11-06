using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;
using Ergo.Solver.DataBindings;
using System.ComponentModel;

namespace Ergo.Solver;

public partial class ErgoSolver : IDisposable
{
    private volatile bool _initialized;

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

    public void Initialize(InterpreterScope interpreterScope)
    {
        _initialized = true;
        var expansions = new Queue<Predicate>();
        var tmpScope = CreateScope(interpreterScope);
        foreach (var pred in KnowledgeBase.ToList())
        {
            foreach (var exp in ExpandPredicate(pred, tmpScope))
            {
                if (!exp.Equals(pred))
                    expansions.Enqueue(exp);
            }
            if (expansions.Count > 0)
            {
                KnowledgeBase.Retract(pred.Head);
                while (expansions.TryDequeue(out var exp))
                {
                    KnowledgeBase.AssertZ(exp);
                }
                expansions.Clear();
            }
        }
    }

    public SolverScope CreateScope(InterpreterScope interpreterScope)
        => new(interpreterScope, 0, interpreterScope.Entry, default, ImmutableArray<Predicate>.Empty, cut: false, new("K"));
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
                    .GetOr(new Dict(sig.Tag.GetEither(WellKnown.Literals.Discard)).CanonicalForm);
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
                        exported: false,
                        tailRecursive: false
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

    // See: https://github.com/G3Kappa/Ergo/issues/36
    public IEnumerable<Predicate> ExpandPredicate(Predicate p, SolverScope scope)
    {
        // Predicates are expanded only once, when they're loaded. The same applies to queries.
        // Expansions are defined as lambdas that define a predicate and capture one variable:
        //   - The head of the predicate is matched against the current term; if they unify:
        //      - The body of the expansion is inserted in the current predicate in a sensible location;
        //      - Previous references to the term are replaced with references to the captured variable.
        foreach (var headExp in ExpandTerm(p.Head, scope))
        {
            var newHead = headExp.Reduce(e => e.Binding
                .Select(v => (ITerm)v).GetOr(e.Match), a => a);
            var headClauses = headExp.Reduce(e => e.Expansion.Contents, _ => ImmutableArray<ITerm>.Empty);
            var bodyExpansions = new List<Either<ExpansionResult, ITerm>>[p.Body.Contents.Length];
            for (int i = 0; i < p.Body.Contents.Length; i++)
            {
                bodyExpansions[i] = new();
                foreach (var bodyExp in ExpandTerm(p.Body.Contents[i], scope))
                    bodyExpansions[i].Add(bodyExp);
                if (bodyExpansions[i].Count == 0)
                    bodyExpansions[i].Add(Either<ExpansionResult, ITerm>.FromB(p.Body.Contents[i]));
            }
            var cartesian = bodyExpansions.CartesianProduct();
            foreach (var variant in cartesian)
            {
                var newBody = new List<ITerm>();
                foreach (var clause in variant)
                {
                    newBody.AddRange(clause.Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>()));
                    newBody.Add(clause.Reduce(e => e.Binding.Select(x => (ITerm)x).GetOr(e.Match), a => a));
                }
                newBody.AddRange(headClauses);
                yield return new Predicate(
                    p.Documentation,
                    p.DeclaringModule,
                    newHead,
                    new(newBody),
                    p.IsDynamic,
                    p.IsExported
                );
            }
        }

        IEnumerable<Either<ExpansionResult, ITerm>> ExpandTerm(ITerm term, SolverScope scope)
        {
            if (term is Variable)
                yield break;
            foreach (var exp in GetExpansions(term, scope)
                .Select(x => Either<ExpansionResult, ITerm>.FromA(x))
                .DefaultIfEmpty(Either<ExpansionResult, ITerm>.FromB(term)))
            {
                // If this is a complex term, expand all of its arguments recursively and produce a combination of all solutions
                if (exp.Reduce(e => e.Match, a => a) is Complex cplx)
                {
                    var expansions = new List<Either<ExpansionResult, ITerm>>[cplx.Arity];
                    for (var i = 0; i < cplx.Arity; i++)
                    {
                        expansions[i] = new();
                        foreach (var argExp in ExpandTerm(cplx.Arguments[i], scope))
                            expansions[i].Add(argExp);
                        if (expansions[i].Count == 0)
                            expansions[i].Add(Either<ExpansionResult, ITerm>.FromB(cplx.Arguments[i]));
                    }
                    var cartesian = expansions.CartesianProduct();
                    foreach (var argList in cartesian)
                    {
                        // TODO: This might mess with abstract forms!
                        var newCplx = cplx.WithArguments(argList
                            .Select(x => x.Reduce(exp => exp.Binding
                               .Select(v => (ITerm)v).GetOr(exp.Match), a => a))
                               .ToImmutableArray());
                        var expClauses = new NTuple(
                            exp.Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>())
                               .Concat(argList.SelectMany(x => x
                                  .Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>()))));
                        yield return Either<ExpansionResult, ITerm>.FromA(new(newCplx, expClauses, exp.Reduce(e => e.Binding, _ => default)));
                    }
                }
                else yield return exp;
            }
        }

        IEnumerable<ExpansionResult> GetExpansions(ITerm term, SolverScope scope)
        {
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
                    // [Output] >> (head :- body(Output)).
                    if (!exp.Predicate.Head.Unify(term).TryGetValue(out var subs))
                        continue;
                    var pred = Predicate.Substitute(exp.Predicate, subs);
                    // Instantiate the OutVariable, but leave the others intact
                    var vars = new Dictionary<string, Variable>();
                    foreach (var var in pred.Head.Variables.Concat(pred.Body.CanonicalForm.Variables)
                        .Where(var => !var.Name.Equals(exp.OutVariable.Name)))
                    {
                        vars[var.Name] = var;
                    }
                    pred = pred.Instantiate(scope.InstantiationContext, vars);
                    yield return new(pred.Head, pred.Body, vars[exp.OutVariable.Name]);
                }
            }
        }
    }

    /// <summary>
    /// Enumerates all implicit qualifications of 'goal' that are worth trying in the current scope.
    /// </summary>
    public static IEnumerable<(ITerm Term, bool Dynamic)> GetImplicitGoalQualifications(ITerm goal, SolverScope scope)
    {
        var isDynamic = false;
        yield return (goal, isDynamic);
        if (!goal.IsQualified)
        {
            {
                var qualified = goal.Qualified(scope.Module);
                if ((isDynamic = scope.InterpreterScope.Modules[scope.Module].DynamicPredicates.Contains(qualified.GetSignature())) || true)
                {
                    yield return (qualified, isDynamic);
                }
            }
            {
                var qualified = goal.Qualified(scope.InterpreterScope.EntryModule.Name);
                if ((isDynamic = scope.InterpreterScope.EntryModule.DynamicPredicates.Contains(qualified.GetSignature())) || true)
                {
                    yield return (qualified, isDynamic);
                }
            }
        }
    }

    public async IAsyncEnumerable<Solution> SolveAsync(Query query, SolverScope scope, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_initialized)
        {
            if (!Flags.HasFlag(SolverFlags.InitializeAutomatically))
                throw new InvalidOperationException("Solver uninitialized. Call InitializeAsync() first.");
            Initialize(scope.InterpreterScope);
        }
        var topLevel = new Predicate(string.Empty, WellKnown.Modules.User, WellKnown.Literals.TopLevel, query.Goals, dynamic: true, exported: false, tailRecursive: false);
        // Solve all *expansions* of the query
        foreach (var exp in ExpandPredicate(topLevel, scope))
        {
            await foreach (var s in new SolverContext(this).SolveAsync(new(exp.Body), scope.WithCallee(exp), ct: ct))
            {
                yield return s;
            }
        }
    }

    public void LogTrace(SolverTraceType type, ITerm term, int depth = 0) => LogTrace(type, () => term.Explain(), depth);
    private void LogTrace(SolverTraceType type, Func<string> s, int depth = 0)
    {
        if (Trace is null || Trace.GetInvocationList().Length == 0)
            return;
        Trace.Invoke(type, $"{type.GetAttribute<DescriptionAttribute>().Description}: ({depth:00}) {s()}");
    }

    public void Dispose()
    {
        Disposing?.Invoke(this);
        GC.SuppressFinalize(this);
    }
}
