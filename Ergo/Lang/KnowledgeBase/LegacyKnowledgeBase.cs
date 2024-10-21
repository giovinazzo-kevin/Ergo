using Ergo.Modules;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Ergo.Lang;

public partial class LegacyKnowledgeBase : IReadOnlyCollection<Clause>
{
    protected readonly OrderedDictionary Predicates = [];

    public readonly InterpreterScope Scope;
    public readonly LegacyDependencyGraph DependencyGraph;

    public LegacyKnowledgeBase(InterpreterScope scope)
    {
        Scope = scope;
        DependencyGraph = new(this);
    }

    private LegacyKnowledgeBase(InterpreterScope scope, OrderedDictionary predicates, LegacyDependencyGraph dependencyGraph)
    {
        Scope = scope;
        Predicates = predicates;
        DependencyGraph = dependencyGraph;
    }

    public int Count => Predicates.Values.Cast<List<Clause>>().Sum(l => l.Count);
    public void Clear() => Predicates.Clear();

    /// <summary>
    /// Removes all non-dynamic, non-cyclical predicates while preserving them in the dependency graph.
    /// </summary>
    public void Trim()
    {
        for (int k = Predicates.Count - 1; k >= 0; k--)
        {
            var list = (List<Clause>)Predicates[k];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var sig = DependencyGraph.GetKey(list[i]);
                if (!DependencyGraph.GetNode(sig).TryGetValue(out var node))
                    Debug.Assert(false);
                if (!node.IsCyclical && !list[i].IsDynamic)
                    list.RemoveAt(i);
            }
            if (list.Count == 0)
                Predicates.RemoveAt(k);
        }
    }

    public LegacyKnowledgeBase Clone()
    {
        var inner = new OrderedDictionary();
        foreach (DictionaryEntry kv in Predicates)
        {
            inner.Add(kv.Key, kv.Value);
        }
        return new(Scope, inner, DependencyGraph);
    }

    private List<Clause> GetOrCreate(Signature key, bool append = false)
    {
        if (!Predicates.Contains(key))
        {
            if (append)
            {
                Predicates.Add(key, new List<Clause>());
            }
            else
            {
                Predicates.Insert(0, key, new List<Clause>());
            }
        }

        return (List<Clause>)Predicates[key];
    }

    private Maybe<List<Clause>> GetImpl(Signature key)
    {
        if (Predicates.Contains(key))
        {
            return Maybe.Some((List<Clause>)Predicates[key]);
        }
        return default;
    }

    public Maybe<IList<Clause>> Get(Signature sig)
    {
        // Direct match
        if (GetImpl(sig).TryGetValue(out var list))
            return Maybe.Some<IList<Clause>>(list);
        // Variadic match (write/*, call/*)
        if (GetImpl(sig.WithArity(default)).TryGetValue(out list))
            return Maybe.Some<IList<Clause>>(list);
        // Matching exported predicates with qualification
        if (sig.Module.TryGetValue(out var module) && Get(sig.WithModule(default)).TryGetValue(out var list_))
            return Maybe.Some<IList<Clause>>(list_.Where(p => p.IsExported || p.DeclaringModule.Equals(module)).ToArray());
        return default;
    }

    public Maybe<IEnumerable<KBMatch>> GetMatches(InstantiationContext ctx, ITerm goal, bool desugar)
    {
        if (desugar)
        {
            // if head is in the form predicate/arity (or its built-in equivalent),
            // do some syntactic de-sugaring and convert it into an actual anonymous complex
            if (goal is Complex c
                && WellKnown.Functors.Division.Contains(c.Functor))
            {
                if (c.Match(out var match, new { Predicate = default(string), Arity = default(int) }))
                {
                    goal = new Atom(match.Predicate).BuildAnonymousTerm(match.Arity);
                }
            }
        }
        // Return predicate matches
        var sig = goal.GetSignature();
        return Get(sig).Select(Inner);
        IEnumerable<KBMatch> Inner(IList<Clause> list)
        {
            var buf = list.ToArray();
            foreach (var k in buf)
            {
                if (k.BuiltIn.TryGetValue(out _))
                {
                    yield return new KBMatch(goal, k, null);
                    yield break;
                }
                // TODO: replace with compiled version of a predicate call
                var predicate = k.Instantiate(ctx);
                if (predicate.Unify(goal).TryGetValue(out var matchSubs))
                {
                    yield return new KBMatch(goal, predicate, matchSubs);
                }
            }
        }
    }

    List<Clause> Assert_(Clause k, bool append)
    {
        var sig = k.Head.GetSignature();
        if (k.IsVariadic)
            sig = sig.WithArity(default);
        return GetOrCreate(sig, append: append);
    }

    public void AssertA(Clause k)
    {
        Assert_(k, append: false).Insert(0, k);
    }
    public void AssertZ(Clause k)
    {
        Assert_(k, append: true).Add(k);
    }
    public bool Retract(ITerm head)
    {
        if (Get(head.GetSignature()).TryGetValue(out var matches))
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var predicate = matches[i];
                if (predicate.IsBuiltIn)
                    continue;
                if (predicate.Unify(head).TryGetValue(out _))
                {
                    matches.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }
    /// <summary>
    /// NOTE: Unlike other flavors of retract, this one allows you to retract built-ins.
    /// </summary>
    public bool Retract(Clause pred)
    {
        if (Get(pred.Head.GetSignature()).TryGetValue(out var matches))
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var predicate = matches[i];
                if (predicate.IsSameDefinitionAs(pred))
                {
                    matches.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }
    public bool Replace(Clause pred, Clause other)
    {
        if (Get(pred.Head.GetSignature()).TryGetValue(out var matches))
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var predicate = matches[i];
                if (predicate.IsSameDefinitionAs(pred))
                {
                    matches.RemoveAt(i);
                    matches.Insert(i, other);
                    return true;
                }
            }
        }

        return false;
    }

    public int RetractAll(ITerm head)
    {
        var retracted = 0;
        if (Get(head.GetSignature()).TryGetValue(out var matches))
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var predicate = matches[i];
                if (predicate.IsBuiltIn)
                    continue;
                if (predicate.Unify(head).TryGetValue(out _))
                {
                    retracted++;
                    matches.RemoveAt(i);
                }
            }
        }

        return retracted;
    }

    public IEnumerator<Clause> GetEnumerator()
    {
        return Predicates.Values
            .Cast<List<Clause>>()
            .SelectMany(l => l)
            .GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

