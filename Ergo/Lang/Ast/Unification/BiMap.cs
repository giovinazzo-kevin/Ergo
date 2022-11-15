using System.Collections;

namespace Ergo.Lang.Ast;

public sealed class BiMap<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
{
    private readonly Dictionary<T1, T2> Fwd = new();
    private readonly Dictionary<T2, T1> Rev = new();

    public void Clear()
    {
        Fwd.Clear();
        Rev.Clear();
    }

    public void Add(T1 left, T2 right)
    {
        Fwd[left] = right;
        Rev[right] = left;
    }
    public void Remove(T1 left)
    {
        Rev.Remove(Fwd[left]);
        Fwd.Remove(left);
    }

    public bool TryGetLvalue(T1 l, out T2 r) => Fwd.TryGetValue(l, out r);
    public bool TryGetRvalue(T2 r, out T1 l) => Rev.TryGetValue(r, out l);

    public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => ((IEnumerable<KeyValuePair<T1, T2>>)Fwd).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Fwd).GetEnumerator();
}
