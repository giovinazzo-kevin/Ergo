using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    private bool @readonly = false;
    public static readonly SubstitutionMap Empty = new() { @readonly = true };
    public static readonly Pool<SubstitutionMap> Pool = new(() => new(), q => q.Clear(), filter: q => !q.@readonly);

    private Dictionary<ITerm, ITerm> Map = new();

    public ITerm this[ITerm key] => Map[key];

    public SubstitutionMap() { }
    public SubstitutionMap(IEnumerable<Substitution> source) { AddRange(source); }

    public void Clear()
    {
        if (@readonly)
            throw new InvalidOperationException();

        Map.Clear();
    }

    public SubstitutionMap Clone()
    {
        var e = Pool.Acquire();
        foreach (var item in Map)
            e.Map.Add(item.Key, item.Value);
        return e;
    }

    public static SubstitutionMap MergeCopy(SubstitutionMap A, SubstitutionMap B)
    {
        var newMap = Pool.Acquire();
        newMap.AddRange(A);
        newMap.AddRange(B);
        return newMap;
    }
    public static SubstitutionMap MergeRef(SubstitutionMap A, SubstitutionMap B)
    {
        if (B != null)
            A.AddRange(B);
        SubstitutionMap.Pool.Release(B);
        return A;
    }

    public void Remove(Substitution s)
    {
        if (@readonly)
            throw new InvalidOperationException();

        if (Map.TryGetValue(s.Lhs, out var rhs) && s.Rhs.Equals(rhs))
            Map.Remove(s.Lhs);
    }
    public void RemoveRange(IEnumerable<Substitution> source)
    {
        foreach (var s in source)
            Remove(s);
    }

    public void Add(Substitution s)
    {
        if (@readonly)
            throw new InvalidOperationException();

        Map.Remove(s.Lhs);
        var rhs = s.Rhs;
        while (rhs is Variable v && FollowRef(v, out var prevRhs))
            rhs = prevRhs;
        Map.Add(s.Lhs, rhs);

        bool FollowRef(Variable rhs, out ITerm prevRhs)
        {
            prevRhs = default;
            if (rhs.Ignored && Map.Remove(rhs, out prevRhs))
                return true;
            if (!rhs.Ignored && Map.TryGetValue(rhs, out prevRhs))
                return true;
            return false;
        }
    }
    public void AddRange(IEnumerable<Substitution> source)
    {
        foreach (var s in source)
            Add(s);
    }

    /// <summary>
    /// Removes all substitutions pertaining to variables that are not being explicitly kept.
    /// </summary>
    public void Prune(IEnumerable<Variable> keep)
    {
        if (@readonly)
            throw new InvalidOperationException();

        foreach (var (lhs, rhs) in Map)
        {
            var vars = lhs.Variables.Concat(rhs.Variables);
            if (keep.Intersect(vars).Any())
                continue;
            Map.Remove(lhs);
        }
    }

    public void Invert()
    {
        if (@readonly)
            throw new InvalidOperationException();

        var toAdd = Substitution.QueuePool.Acquire();
        foreach (var (lhs, rhs) in Map)
        {
            if (lhs is not Variable || rhs is not Variable)
                continue;
            Map.Remove(lhs);
            toAdd.Enqueue(new(rhs, lhs));
        }
        while (toAdd.TryDequeue(out var s))
            Add(s);
        Substitution.QueuePool.Release(toAdd);
    }

    public IEnumerator<Substitution> GetEnumerator()
    {
        return Map.Select(x => new Substitution(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
