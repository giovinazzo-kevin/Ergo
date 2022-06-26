using System.Collections;
using System.Collections.Specialized;

namespace Ergo.Lang;

public partial class KnowledgeBase : IReadOnlyCollection<Predicate>
{
    protected readonly OrderedDictionary Predicates;
    protected readonly InstantiationContext Context;

    public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

    public KnowledgeBase()
    {
        Predicates = new OrderedDictionary();
        Context = new("K");
    }

    public void Clear() => Predicates.Clear();

    private List<Predicate> GetOrCreate(Signature key, bool append = false)
    {
        if (!Predicates.Contains(key))
        {
            if (append)
            {
                Predicates.Add(key, new List<Predicate>());
            }
            else
            {
                Predicates.Insert(0, key, new List<Predicate>());
            }
        }

        return (List<Predicate>)Predicates[key];
    }

    public Maybe<List<Predicate>> Get(Signature key)
    {
        if (Predicates.Contains(key))
        {
            return Maybe.Some((List<Predicate>)Predicates[key]);
        }

        return default;
    }

    public IEnumerable<KBMatch> GetMatches(ITerm goal, bool desugar)
    {
        if (desugar)
        {
            // if head is in the form predicate/arity (or its built-in equivalent),
            // do some syntactic de-sugaring and convert it into an actual anonymous complex
            if (goal is Complex c
                && WellKnown.Functors.Division.Contains(c.Functor)
                && c.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
            {
                goal = new Atom(match.Predicate).BuildAnonymousTerm(match.Arity);
            }
        }
        // Instantiate goal
        var inst = goal.Instantiate(Context);
        if (!inst.Unify(goal).TryGetValue(out var subs))
            return Enumerable.Empty<KBMatch>();

        var head = goal.Substitute(subs);
        // Return predicate matches
        if (Get(head.GetSignature()).TryGetValue(out var list))
            return Inner(list);

        if (head.IsQualified && head.GetQualification(out head).TryGetValue(out var module) && Get(head.GetSignature()).TryGetValue(out list))
            return Inner(list).Where(p => p.Rhs.IsExported && p.Rhs.DeclaringModule.Equals(module));

        return Enumerable.Empty<KBMatch>();
        IEnumerable<KBMatch> Inner(List<Predicate> list)
        {
            foreach (var k in list)
            {
                var predicate = k.Instantiate(Context);
                if (predicate.Unify(head).TryGetValue(out var matchSubs))
                {
                    predicate = Predicate.Substitute(predicate, matchSubs);
                    yield return new KBMatch(head, predicate, matchSubs.Concat(subs));
                }
            }
        }
    }

    public void AssertA(Predicate k) => GetOrCreate(k.Head.GetSignature(), append: false).Insert(0, k);

    public void AssertZ(Predicate k) => GetOrCreate(k.Head.GetSignature(), append: true).Add(k);

    public bool Retract(ITerm head)
    {
        if (Get(head.GetSignature()).TryGetValue(out var matches))
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var predicate = matches[i];
                if (predicate.Unify(head).TryGetValue(out _))
                {
                    matches.RemoveAt(i);
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
                if (predicate.Unify(head).TryGetValue(out _))
                {
                    retracted++;
                    matches.RemoveAt(i);
                }
            }
        }

        return retracted;
    }

    public IEnumerator<Predicate> GetEnumerator()
    {
        return Predicates.Values
            .Cast<List<Predicate>>()
            .SelectMany(l => l)
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

