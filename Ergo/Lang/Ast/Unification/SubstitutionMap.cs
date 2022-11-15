using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class SubstitutionMap : IEnumerable<Substitution>
{
    public int Count { get; private set; }

    private readonly Dictionary<ITerm, ITerm> Forward = new();
    private readonly Dictionary<ITerm, ITerm> Reverse = new();

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
        if (Reverse.TryGetValue(s.Lhs, out var prevLhs))
        {
            Reverse.Remove(s.Lhs);
            Forward.Remove(prevLhs);
            Forward.Add(prevLhs, s.Rhs);
            Reverse.Add(s.Rhs, prevLhs);
        }
        else if (Forward.TryGetValue(s.Rhs, out var prevRhs))
        {
            Forward.Remove(s.Rhs);
            Reverse.Remove(prevRhs);
            Forward.Add(s.Lhs, prevRhs);
            Reverse.Add(prevRhs, s.Lhs);
        }
        else
        {
            Forward.Add(s.Lhs, s.Rhs);
            if (s.Rhs is Variable)
                Reverse.Add(s.Rhs, s.Lhs);
            ++Count;
        }
    }
    public void AddRange(IEnumerable<Substitution> source)
    {
        foreach (var s in source)
            Add(s);
    }

    public IEnumerator<Substitution> GetEnumerator() => Forward.Select(kv => new Substitution(kv.Key, kv.Value)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
