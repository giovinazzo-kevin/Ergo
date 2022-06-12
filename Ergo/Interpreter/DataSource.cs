using System.Threading;
using System.Threading.Tasks;

namespace Ergo.Interpreter;

public enum RejectionData
{
    /// <summary>
    /// Items that fail unification when pulling from this data source will be discarded.
    /// </summary>
    Discard,
    /// <summary>
    /// When an item fails unification it will be stored and re-emitted perpetually until it gets unified with something else.
    /// </summary>
    Repeat,
    /// <summary>
    /// When an item fails unification it will be stowed away in a queue and ignored for the context of the current call. It will then be re-emitted on the next call, together with all other rejects.
    /// </summary>
    Recycle
}

public enum RejectionControl
{
    /// <summary>
    /// When an item fails unification the enumeration continues.
    /// </summary>
    Continue,
    /// <summary>
    /// When an item fails unification the enumeration breaks.
    /// </summary>
    Break
}

public sealed class DataSource : IAsyncEnumerable<ITerm>
{
    private readonly (Queue<ITerm> Front, Queue<ITerm> Back) _queues = (new(), new());
    private readonly Func<IAsyncEnumerable<ITerm>> Source;
    public event Action<DataSource, ITerm> ItemYielded;

    public readonly RejectionData DataSemantics;
    public readonly RejectionControl ControlSemantics;

    public bool Reject(ITerm item)
    {
        switch (DataSemantics)
        {
            case RejectionData.Discard:
                break;
            case RejectionData.Repeat:
                if (_queues.Front.Count != 0) throw new InvalidOperationException();
                _queues.Front.Enqueue(item);
                break;
            case RejectionData.Recycle:
                _queues.Back.Enqueue(item);
                break;
        }

        return ControlSemantics == RejectionControl.Break;
    }

    private Maybe<ITerm> GetReject()
    {
        switch (DataSemantics)
        {
            case RejectionData.Discard:
                return Maybe<ITerm>.None;
            case RejectionData.Repeat:
                if (_queues.Front.Count == 0) return Maybe<ITerm>.None;
                if (_queues.Front.Count != 1 || !_queues.Front.TryDequeue(out var a)) throw new InvalidOperationException();
                return Maybe.Some(a);
            case RejectionData.Recycle:
                if (_queues.Front.TryDequeue(out var b)) return Maybe.Some(b);
                return Maybe<ITerm>.None;
        }

        return Maybe<ITerm>.None;
    }

    private void ProcessBackQueue()
    {
        while (_queues.Back.TryDequeue(out var reject))
        {
            _queues.Front.Enqueue(reject);
        }
    }

    async IAsyncEnumerable<ITerm> FromEnumerable(Func<IEnumerable<ITerm>> data)
    {
        ProcessBackQueue();
        while (GetReject().TryGetValue(out var reject))
            yield return reject;
        foreach (var item in data())
        {
            yield return item;
            ItemYielded?.Invoke(this, item);
            while (GetReject().TryGetValue(out var reject))
                yield return reject;
        }

        await Task.CompletedTask;
    }

    async IAsyncEnumerable<ITerm> FromAsyncEnumerable(Func<IAsyncEnumerable<ITerm>> data)
    {
        ProcessBackQueue();
        while (GetReject().TryGetValue(out var reject))
            yield return reject;
        await foreach (var item in data())
        {
            yield return item;
            ItemYielded?.Invoke(this, item);
            while (GetReject().TryGetValue(out var reject))
                yield return reject;
        }
    }

    public IAsyncEnumerator<ITerm> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Source().GetAsyncEnumerator(cancellationToken);

    public DataSource(Func<IEnumerable<ITerm>> source, RejectionData rejectSemantics = RejectionData.Discard, RejectionControl enumSemantics = RejectionControl.Continue)
    {
        DataSemantics = rejectSemantics;
        ControlSemantics = enumSemantics;
        Source = () => FromEnumerable(source);
    }

    public DataSource(Func<IAsyncEnumerable<ITerm>> source, RejectionData dataSemantics = RejectionData.Discard, RejectionControl ctrlSemantics = RejectionControl.Continue)
    {
        DataSemantics = dataSemantics;
        ControlSemantics = ctrlSemantics;
        Source = () => FromAsyncEnumerable(source);
    }
}
