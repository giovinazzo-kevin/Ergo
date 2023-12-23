using Ergo.Interpreter;
using System.Collections;

namespace Ergo.Lang;


public partial class KnowledgeBase
{
    public record struct Clause(Predicate Predicate, ulong AssertedOn, ulong DeletedOn = ulong.MaxValue)
    {
        public readonly Clause Retracted(ulong gen) => new(Predicate, AssertedOn, gen);
    }

    public class Record
    {
        public readonly List<Clause> Clauses = new();
    }

}

public partial class KnowledgeBase(KnowledgeBase clone) : IEnumerable<Predicate>
{
    public KnowledgeBase(InterpreterScope scope)
        : this(null)
    {
        Scope = scope;
        DependencyGraph = new(this);
    }

    public readonly InterpreterScope Scope;
    public readonly DependencyGraph DependencyGraph = clone?.DependencyGraph.Clone(clone);

    protected readonly Dictionary<int, Record> Index = clone?.Index.ToDictionary() ?? new();
    protected ulong CurrentGeneration = clone?.CurrentGeneration ?? 0;
    public int Count { get; private set; }

    public IEnumerable<Clause> Get(ITerm variant, out Record index)
    {
        var hashCode = variant.GetVariantHashCode();
        if (Index.TryGetValue(hashCode, out index))
            return index.Clauses
                .Where(x => CurrentGeneration < x.DeletedOn);
        return Enumerable.Empty<Clause>();
    }

    public IEnumerable<KBMatch> Match(ITerm variant, InstantiationContext ctx)
    {
        foreach (var clause in Get(variant, out _))
        {
            var pred = clause.Predicate;
            if (pred.BuiltIn.TryGetValue(out _))
            {
                yield return new KBMatch(variant, pred, null);
                yield break;
            }
            else if (pred.Instantiate(ctx).Unify(variant).TryGetValue(out var subs))
            {
                yield return new KBMatch(variant, pred, subs);
            }
        }
    }

    void Assert_(Predicate clause, bool z)
    {
        Count++;
        var gen = CurrentGeneration++;
        if (clause.IsVariadic)
        {
            var sig = clause.Head.GetSignature();
            // Hash the first N arguments (variadics can't be asserted at runtime, so the perf impact is minimal)
            for (int arity = 0; arity < ErgoVM.MAX_ARGUMENTS; ++arity)
            {
                AssertNonVariadic(sig.Functor.BuildAnonymousTerm(arity, ignoredVars: false));
            }
        }
        else AssertNonVariadic(clause.Head);
        if (!clause.IsDynamic)
        {
            DependencyGraph.AddNode(clause);
            DependencyGraph.CalculateDependencies(clause);
        }
        void AssertNonVariadic(ITerm head)
        {
            var hashCode = head.GetVariantHashCode();
            if (!Index.TryGetValue(hashCode, out var index))
                index = Index[hashCode] = new();
            if (z)
                index.Clauses.Add(new(clause, gen));
            else
                index.Clauses.Insert(0, new(clause, gen));
        }
    }

    public void AssertA(Predicate clause)
    {
        Assert_(clause, false);
    }

    public void AssertZ(Predicate clause)
    {
        Assert_(clause, true);
    }
    public IEnumerable<Predicate> Retract(ITerm variant, bool isItRuntime)
    {
        var currentGen = CurrentGeneration;
        var hashCode = variant.GetVariantHashCode();
        if (Index.TryGetValue(hashCode, out var index))
        {
            for (int i = index.Clauses.Count - 1; i >= 0; i--)
            {
                var k = index.Clauses[i];
                if (!isItRuntime && !k.Predicate.IsDynamic)
                {
                    yield return k.Predicate; // Let the VM handle it
                    continue;
                }
                if (currentGen < k.DeletedOn)
                {
                    index.Clauses[i] = k.Retracted(CurrentGeneration++);
                    if (DependencyGraph.GetNode(DependencyGraph.GetKey(k.Predicate)).TryGetValue(out var node))
                        node.Clauses.Remove(k.Predicate);
                    if (!isItRuntime)
                        DependencyGraph.CalculateDependencies(k.Predicate);
                    Count--;
                    yield return k.Predicate;
                }
            }
        }
    }
    public int RetractAll(ITerm variant, bool isItRuntime) => Retract(variant, isItRuntime).Count();


    /// <summary>
    /// NOTE: Do not call at runtime or it breaks the logical update view.
    /// </summary>
    public bool Replace(Predicate pred, Predicate other)
    {
        foreach (var match in Get(pred.Head, out var index))
        {
            if (match.Predicate.IsSameDefinitionAs(pred))
            {
                var i = index.Clauses.IndexOf(match);
                index.Clauses.RemoveAt(i);
                index.Clauses.Insert(i, new(other, CurrentGeneration++));
                DependencyGraph.CalculateDependencies(match.Predicate);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// NOTE: Do not call at runtime or it breaks the logical update view.
    /// </summary>
    public bool Remove(Predicate pred)
    {
        foreach (var match in Get(pred.Head, out var index))
        {
            if (match.Predicate.IsSameDefinitionAs(pred))
            {
                index.Clauses.Remove(match);
                DependencyGraph.CalculateDependencies(match.Predicate);
                Count--;
                return true;
            }
        }
        return false;
    }

    public IEnumerator<Predicate> GetEnumerator()
    {
        var gen = CurrentGeneration;
        foreach (var k in Index.Values.SelectMany(x => x.Clauses)
            .Where(k => gen < k.DeletedOn))
        {
            yield return k.Predicate;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}