﻿using System.Collections;
using System.Collections.Specialized;

namespace Ergo.Lang;

public partial class KnowledgeBase : IReadOnlyCollection<Predicate>
{
    protected readonly OrderedDictionary Predicates = new();

    public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

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

    private Maybe<List<Predicate>> GetImpl(Signature key)
    {
        if (Predicates.Contains(key))
        {
            return Maybe.Some((List<Predicate>)Predicates[key]);
        }

        return default;
    }

    public Maybe<IList<Predicate>> Get(Signature sig)
    {
        if (GetImpl(sig).TryGetValue(out var list))
            return Maybe.Some<IList<Predicate>>(list);
        if (sig.Module.TryGetValue(out var module) && Get(sig.WithModule(default)).TryGetValue(out var list_))
            return Maybe.Some<IList<Predicate>>(list_.Where(p => p.IsExported && p.DeclaringModule.Equals(module)).ToArray());
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
                if (c.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
                {
                    goal = new Atom(match.Predicate).BuildAnonymousTerm(match.Arity);
                }
            }
        }
        // Return predicate matches
        var sig = goal.GetSignature();
        return Get(sig).Select(Inner);
        IEnumerable<KBMatch> Inner(IList<Predicate> list)
        {
            var buf = list.ToArray();
            return buf
                .Select(k =>
                {
                    var predicate = k.Instantiate(ctx);
                    if (predicate.IsVariadic && predicate.Head.GetFunctor().TryGetValue(out var fun))
                    {
                        predicate = predicate.WithHead(fun.BuildAnonymousTerm(goal.GetArguments().Length));
                    }
                    if (predicate.Unify(goal).TryGetValue(out var matchSubs))
                    {
                        predicate = Predicate.Substitute(predicate, matchSubs);
                        return Maybe.Some(new KBMatch(goal, predicate, matchSubs));
                    }
                    return default;
                })
                .Where(x => x.TryGetValue(out _))
                .Select(x => x.GetOrThrow(new InvalidOperationException()));
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
    public bool Retract(Predicate pred)
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

    public IEnumerator<Predicate> GetEnumerator()
    {
        return Predicates.Values
            .Cast<List<Predicate>>()
            .SelectMany(l => l)
            .GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

