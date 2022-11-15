using Anvoker.Maps;
using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    private readonly CompositeBiMap<ITerm, ITerm> Map = new();

    public SubstitutionMap() { }
    public SubstitutionMap(IEnumerable<Substitution> source) { AddRange(source); }

    public static SubstitutionMap MergeCopy(SubstitutionMap A, SubstitutionMap B)
    {
        var newMap = new SubstitutionMap();
        newMap.AddRange(A);
        newMap.AddRange(B);
        return newMap;
    }
    public static SubstitutionMap MergeRef(ref SubstitutionMap A, SubstitutionMap B)
    {
        A.AddRange(B);
        return A;
    }

    public void Remove(Substitution s)
    {
        Map.Remove(s.Lhs);
    }

    public void Add(Substitution s)
    {
        //if (Reverse.TryGetValue(s.Lhs, out var prevLhs))
        //{
        //    Reverse.Remove(s.Lhs);
        //    Forward.Remove(prevLhs);
        //    Forward.Add(prevLhs, s.Rhs);
        //    Reverse.Add(s.Rhs, prevLhs);
        //}
        //else if (Forward.TryGetValue(s.Rhs, out var prevRhs))
        //{
        //    Forward.Remove(s.Rhs);
        //    Reverse.Remove(prevRhs);
        //    Forward.Add(s.Lhs, prevRhs);
        //    Reverse.Add(prevRhs, s.Lhs);
        //}
        //else
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
