using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    private readonly BiMap<ITerm, ITerm> Map = new();

    public SubstitutionMap() { }
    public SubstitutionMap(IEnumerable<Substitution> source) { AddRange(source); }

    public void Clear()
    {
        Map.Clear();
    }

    public static SubstitutionMap MergeCopy(SubstitutionMap A, SubstitutionMap B)
    {
        var newMap = new SubstitutionMap();
        newMap.AddRange(A);
        newMap.AddRange(B);
        return newMap;
    }
    public static SubstitutionMap MergeRef(SubstitutionMap A, SubstitutionMap B)
    {
        A.AddRange(B);
        return A;
    }

    public void Add(Substitution s)
    {
        if (Map.TryGetRvalue(s.Lhs, out var prevLhs))
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

    public IEnumerator<Substitution> GetEnumerator()
    {
        return Map.Select(x => new Substitution(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
