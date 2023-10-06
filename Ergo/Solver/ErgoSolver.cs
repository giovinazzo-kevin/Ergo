using Ergo.Events.Solver;
using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Solver.DataBindings;
using System.IO;

namespace Ergo.Solver;

// TODO: make thread safe
public partial class ErgoSolver : IDisposable
{
    private volatile bool _initialized;

    public readonly SolverFlags Flags;

    public readonly ErgoFacade Facade;
    public readonly KnowledgeBase KnowledgeBase;
    public readonly HashSet<Atom> DataSinks = new();
    public readonly Dictionary<Signature, HashSet<DataSource>> DataSources = new();

    public TextReader In { get; private set; }
    public TextWriter Out { get; private set; }
    public TextWriter Err { get; private set; }

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
        In = Console.In;
        Out = Console.Out;
        Err = Console.Error;
    }

    public void Initialize(InterpreterScope scope)
    {
        _initialized = true;
        scope.ForwardEventToLibraries(new SolverInitializingEvent(this, scope));
    }

    public SolverScope CreateScope(InterpreterScope interpreterScope)
        => new(interpreterScope, interpreterScope.Entry, new("K"), new());
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
            sink?.Disconnect(this);
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

    /// <summary>
    /// Enumerates all implicit qualifications of 'goal' that are worth trying in the current scope.
    /// </summary>
    public static IEnumerable<ITerm> GetImplicitGoalQualifications(ITerm goal, SolverScope scope)
    {
        yield return goal;
        if (!goal.IsQualified)
        {
            foreach (var m in scope.InterpreterScope.VisibleModules)
            {
                yield return goal.Qualified(m);
            }
        }
    }

    public IEnumerable<Solution> Solve(Query query, SolverScope scope, CancellationToken ct = default)
    {
        if (!_initialized)
        {
            if (!Flags.HasFlag(SolverFlags.InitializeAutomatically))
                throw new InvalidOperationException("Solver uninitialized. Call Initialize() first.");
            Initialize(scope.InterpreterScope);
        }
        var topLevelHead = new Complex(WellKnown.Literals.TopLevel, query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
        var topLevel = new Predicate(string.Empty, WellKnown.Modules.User, topLevelHead, query.Goals, dynamic: true, exported: false, tailRecursive: false);
        // Assert the top level query as a predicate, so that libraries are free to transform it
        KnowledgeBase.AssertA(topLevel);
        scope.InterpreterScope.ForwardEventToLibraries(new QuerySubmittedEvent(this, query, scope));
        var queryExpansions = KnowledgeBase
            .GetMatches(scope.InstantiationContext, topLevelHead, desugar: false)
            .AsEnumerable()
            .SelectMany(x => x)
            .ToList();
        KnowledgeBase.RetractAll(topLevelHead);
        foreach (var exp in queryExpansions)
        {
            using var ctx = SolverContext.Create(this, scope.InterpreterScope);
            var newPred = Predicate.Substitute(exp.Rhs, exp.Substitutions.Select(x => x.Inverted()));
            foreach (var s in ctx.Solve(new(newPred.Body), scope.WithCallee(newPred), ct: ct))
            {
                yield return s;
            }
        }
    }

    public void SetIn(TextReader newIn)
    {
        if (newIn is null)
            throw new ArgumentNullException(nameof(newIn));
        In = TextReader.Synchronized(newIn);
    }

    public void SetOut(TextWriter newOut)
    {
        if (newOut is null)
            throw new ArgumentNullException(nameof(newOut));
        Out = TextWriter.Synchronized(newOut);
    }

    public void SetErr(TextWriter newErr)
    {
        if (newErr is null)
            throw new ArgumentNullException(nameof(newErr));
        Err = TextWriter.Synchronized(newErr);
    }

    public void Dispose()
    {
        Disposing?.Invoke(this);
        GC.SuppressFinalize(this);
    }
}
