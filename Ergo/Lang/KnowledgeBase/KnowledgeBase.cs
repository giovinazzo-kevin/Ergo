using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace Ergo.Lang
{
    public partial class KnowledgeBase : IReadOnlyCollection<Predicate>
    {
        protected readonly ImmutableDictionary<Signature, HashSet<DataSource>> DataSources;
        protected readonly OrderedDictionary Predicates;
        protected readonly InstantiationContext Context;

        public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

        public KnowledgeBase(Dictionary<Signature, HashSet<DataSource>> dataSources = null)
        {
            DataSources = ImmutableDictionary.CreateRange(dataSources ?? Enumerable.Empty<KeyValuePair<Signature, HashSet<DataSource>>>());
            Predicates = new OrderedDictionary();
            Context = new("K");
        }

        public void Clear() => Predicates.Clear();

        private List<Predicate> GetOrCreate(Signature key, bool append=false)
        {
            if (!Predicates.Contains(key)) {
                if(append) {
                    Predicates.Add(key, new List<Predicate>());
                }
                else {
                    Predicates.Insert(0, key, new List<Predicate>());
                }
            }
            return (List<Predicate>)Predicates[key];
        }

        public bool TryGet(Signature key, out List<Predicate> predicates)
        {
            predicates = default;
            if (Predicates.Contains(key)) {
                predicates = (List<Predicate>)Predicates[key];
                return true;
            }
            var looseKey = key.WithModule(Maybe<Atom>.None);
            if (key.Module.HasValue && Predicates.Contains(looseKey))
            {
                predicates = (List<Predicate>)Predicates[looseKey];
                return predicates.All(p => p.DeclaringModule.Equals(key.Module.GetOrThrow()));
            }
            return false;
        }

        public async IAsyncEnumerable<Match> GetDataSourceMatches(ITerm goal)
        {
            // Instantiate goal
            if (!new Substitution(goal.Instantiate(Context), goal).TryUnify(out var subs))
            {
                yield break;
            }
            var head = goal.Substitute(subs);
            var signature = head.GetSignature();
            // Return results from data sources 
            if (DataSources.TryGetValue(signature.WithModule(Maybe.Some(Modules.CSharp)), out var sources))
            {
                foreach (var source in sources)
                {
                    await foreach (var item in source)
                    {
                        var predicate = new Predicate(
                            "data source",
                            Modules.CSharp,
                            item.WithFunctor(signature.Functor),
                            CommaSequence.Empty,
                            dynamic: true
                        ).Instantiate(Context);
                        if (Predicate.TryUnify(head, predicate, out var matchSubs))
                        {
                            predicate = Predicate.Substitute(predicate, matchSubs);
                            yield return new Match(goal, predicate, matchSubs.Concat(subs));
                        }
                    }
                }
            }
        }

        public IEnumerable<Match> GetMatches(ITerm goal)
        {
            // Instantiate goal
            if(!new Substitution(goal.Instantiate(Context), goal).TryUnify(out var subs)) {
                yield break;
            }
            var head = goal.Substitute(subs);
            var signature = head.GetSignature();
            // Return predicate matches
            if (TryGet(signature, out var list)) {
                foreach (var k in list) {
                    var predicate = k.Instantiate(Context);
                    if (Predicate.TryUnify(head, predicate, out var matchSubs))
                    {
                        predicate = Predicate.Substitute(predicate, matchSubs);
                        yield return new Match(goal, predicate, matchSubs.Concat(subs));
                    }
                }
            }
        }

        public async IAsyncEnumerable<Match> GetMatchesAndDataSourceMatches(ITerm goal)
        {
            foreach (var match in GetMatches(goal))
            {
                yield return match;
            }
            await foreach(var match in GetDataSourceMatches(goal))
            {
                yield return match;
            }
        }

        public void AssertA(Predicate k)
        {
            GetOrCreate(k.Head.GetSignature(), append: false).Insert(0, k);
        }

        public void AssertZ(Predicate k)
        {
            GetOrCreate(k.Head.GetSignature(), append: true).Add(k);
        }

        public bool Retract(ITerm head)
        {
            if (TryGet(head.GetSignature(), out var matches)) {
                for (int i = matches.Count - 1; i >= 0; i--) {
                    var predicate = matches[i];
                    if (Predicate.TryUnify(head, predicate, out _)) {
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
            if (TryGet(head.GetSignature(), out var matches)) {
                for (int i = matches.Count - 1; i >= 0; i--) {
                    var predicate = matches[i];
                    if (Predicate.TryUnify(head, predicate, out _)) {
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
