namespace Ergo.Lang.Ast;

public class Pool<T>(Func<T> init, Action<T> final = null, Func<T, bool> filter = null)
{
    private readonly ThreadLocal<Stack<T>> _stack = new(() => new());
    private readonly Func<T> _initialize = init;
    private readonly Action<T> _finalize = final ?? (_ => { });
    private readonly Func<T, bool> _filter = filter ?? (_ => true);

    public T Acquire()
    {
        if (_stack.Value.TryPop(out var res))
            return res;
        return _initialize();
    }

    public void Release(T t)
    {
        if (!_filter(t))
            return;
        _finalize(t);
        _stack.Value.Push(t);
    }
}
