using System.Collections;
using System.Collections.Specialized;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    public int Count { get; private set; }

    private readonly OrderedDictionary Forward = new();
    private readonly OrderedDictionary Reverse = new();

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
        Forward.Remove(s.Lhs);
        Reverse.Remove(s.Rhs);
        --Count;
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
            Forward.Add(s.Lhs, s);
            if (s.Rhs is Variable)
                Reverse.Add(s.Rhs, s);
            ++Count;
        }
    }
    public void AddRange(IEnumerable<Substitution> source)
    {
        foreach (var s in source)
            Add(s);
    }

    public IEnumerator<Substitution> GetEnumerator()
    {
        return Inner().GetEnumerator();
        IEnumerable<Substitution> Inner()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return (Substitution)Forward[i];
            }
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
