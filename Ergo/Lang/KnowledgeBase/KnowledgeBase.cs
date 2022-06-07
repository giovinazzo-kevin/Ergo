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
        protected readonly ImmutableDictionary<Signature, HashSet<IEnumerable<ITerm>>> DataSources;
        protected readonly OrderedDictionary Predicates;
        protected readonly InstantiationContext Context;

        public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

        public KnowledgeBase(Dictionary<Signature, HashSet<IEnumerable<ITerm>>> dataSources = null)
        {
            DataSources = ImmutableDictionary.CreateRange(dataSources ?? Enumerable.Empty<KeyValuePair<Signature, HashSet<IEnumerable<ITerm>>>>());
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
            return false;
        }

        public bool TryGetMatches(ITerm goal, out IEnumerable<Match> matches)
        {
            var lst = new List<Match>();
            matches = lst;
            // Instantiate goal
            if(!new Substitution(goal.Instantiate(Context), goal).TryUnify(out var subs)) {
                return false;
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
                        lst.Add(new Match(goal, predicate, matchSubs.Concat(subs)));
                    }
                }
            }
            // Return results from data sources 
            if(DataSources.TryGetValue(signature, out var sources))
            {
                foreach (var item in sources.SelectMany(i => i))
                {
                    var predicate = new Predicate(
                        "data source",
                        signature.Module.Reduce(some => some, () => Modules.User),
                        item,
                        CommaSequence.Empty,
                        dynamic: true
                    ).Instantiate(Context);
                    if (Predicate.TryUnify(head, predicate, out var matchSubs))
                    {
                        predicate = Predicate.Substitute(predicate, matchSubs);
                        lst.Add(new Match(goal, predicate, matchSubs.Concat(subs)));
                    }
                }
            }
            return lst.Any();
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
