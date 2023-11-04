using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    private readonly BiMap<ITerm, ITerm> Map = new();

    public ITerm this[ITerm key] => Map[key];

    public SubstitutionMap() { }
    public SubstitutionMap(IEnumerable<Substitution> source) { AddRange(source); }

    public void Clear()
    {
        Map.Clear();
    }

    public static SubstitutionMap MergeCopy(SubstitutionMap A, SubstitutionMap B)
    {
        var newMap = Substitution.Pool.Acquire();
        newMap.AddRange(A);
        newMap.AddRange(B);
        return newMap;
    }
    public static SubstitutionMap MergeRef(SubstitutionMap A, SubstitutionMap B)
    {
        if (B != null)
            A.AddRange(B);
        return A;
    }

    public void Add(Substitution s)
    {
        if (s.Lhs is Variable { Ignored: true } && Map.TryGetRvalue(s.Lhs, out var prevLhs))
        {
            Map.Remove(prevLhs);
            Map.Add(prevLhs, s.Rhs);
        }
        else if (s.Rhs is Variable { Ignored: true } && Map.TryGetLvalue(s.Rhs, out var prevRhs))
        {
            Map.Remove(s.Rhs);
            Map.Add(s.Lhs, prevRhs);
        }
        else if (!s.Rhs.Equals(WellKnown.Literals.Discard))
        {
            Map.Add(s.Lhs, s.Rhs);
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
            if (keep.Intersect(vars).Any() || vars.Any(v => !v.Ignored))
                continue;
            Map.Remove(lhs);
        }
    }

    public void Invert()
    {
        var toAdd = Substitution.QueuePool.Acquire();
        foreach (var (lhs, rhs) in Map)
        {
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
