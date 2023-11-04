namespace Ergo.Lang.Ast;

public class Pool<T>
{
    private readonly ThreadLocal<Stack<T>> _stack = new(() => new());
    private readonly Func<T> _initialize;
    private readonly Action<T> _finalize;

    public Pool(Func<T> init, Action<T> final = null)
    {
        _initialize = init;
        _finalize = final ?? (_ => { });
    }

    public T Acquire()
    {
        if (_stack.Value.TryPop(out var res))
            return res;
        return _initialize();
    }

    public void Release(T t)
    {
        _finalize(t);
        _stack.Value.Push(t);
    }
}
