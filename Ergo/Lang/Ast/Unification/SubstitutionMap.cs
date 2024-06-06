using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    public static readonly Pool<SubstitutionMap> Pool = new(() => [], q => q.Clear());

    private Dictionary<Variable, ITerm> Map = [];

    public ITerm this[Variable key] => Map[key];

    public SubstitutionMap() { }
    public SubstitutionMap(IEnumerable<Substitution> source) { AddRange(source); }

    public void Clear()
    {
        Map.Clear();
    }

    public SubstitutionMap Clone()
    {
        var e = Pool.Acquire();
        e.Map = new(Map);
        return e;
    }

    public static SubstitutionMap MergeRef(SubstitutionMap A, SubstitutionMap B)
    {
        if (B != null)
            A.AddRange(B);
        Pool.Release(B);
        return A;
    }

    public void Remove(Substitution s)
    {
        if (Map.TryGetValue((Variable)s.Lhs, out var rhs) && s.Rhs.Equals(rhs))
            Map.Remove((Variable)s.Lhs);
    }
    public void RemoveRange(IEnumerable<Substitution> source)
    {
        foreach (var s in source)
            Remove(s);
    }

    public void Add(Substitution s)
    {
        Map.Remove((Variable)s.Lhs);
        var rhs = s.Rhs;
        while (rhs is Variable v && FollowRef(v, out var prevRhs))
            rhs = prevRhs;
        Map.Add((Variable)s.Lhs, rhs);
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
        var toAdd = Substitution.QueuePool.Acquire();
        foreach (var (lhs, rhs) in Map)
        {
            if (rhs is not Variable)
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
