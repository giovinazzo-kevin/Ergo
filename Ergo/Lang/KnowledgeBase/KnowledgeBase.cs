using Ergo.Lang.Ast;
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
        protected readonly OrderedDictionary Predicates;
        protected readonly InstantiationContext Context;

        public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

        public KnowledgeBase()
        {
            Predicates = new OrderedDictionary();
            Context = new("K");
        }

        private List<Predicate> GetOrCreate(string key, bool append=false)
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

        public bool TryGet(string key, out List<Predicate> predicates)
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
            if (TryGet(Predicate.Signature(head), out var list)) {
                foreach (var k in list) {
                    var ks = k.Instantiate(Context);
                    if (Predicate.TryUnify(head, ks, out var matchSubs)) {
                        ks = Predicate.Substitute(ks, matchSubs);
                        lst.Add(new Match(goal, ks, matchSubs.Concat(subs)));
                    }
                }
                return true;
            }
            return false;
        }

        public void AssertA(Predicate k)
        {
            GetOrCreate(Predicate.Signature(k.Head), append: false).Insert(0, k);
        }

        public void AssertZ(Predicate k)
        {
            GetOrCreate(Predicate.Signature(k.Head), append: true).Add(k);
        }

        public bool RetractOne(ITerm head)
        {
            if (TryGet(Predicate.Signature(head), out var matches)) {
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
            if (TryGet(Predicate.Signature(head), out var matches)) {
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
