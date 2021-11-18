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
        public int Count => Predicates.Values.Cast<List<Predicate>>().Sum(l => l.Count);

        public KnowledgeBase()
        {
            Predicates = new OrderedDictionary();
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

        public bool TryGetMatches(Term head, out IEnumerable<Match> matches)
        {
            var lst = new List<Match>();
            matches = lst;
            if (TryGet(Predicate.Signature(head), out var list)) {
                foreach (var k in list) {
                    if(Predicate.TryUnify(head, k, out var subs)) {
                        // Instantiate and unify predicate head
                        var inst = Term.Instantiate(new Term.InstantiationContext(), k.Head);
                        if (!Substitution.TryUnify(new Substitution(head, inst), out var instSubs)) {
                            throw new InvalidOperationException("Unification between term and its instantiation failed.");
                        }
                        var allSubs = instSubs.Concat(subs).Distinct();
                        var ks = Predicate.Substitute(k, allSubs);
                        lst.Add(new Match(head, ks, allSubs));
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

        public bool RetractOne(Term head)
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

        public int RetractAll(Term head)
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
