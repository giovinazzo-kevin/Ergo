using Ergo.Solver.DataBindings.Interfaces;
using System.Threading.Channels;

namespace Ergo.Solver.DataBindings;

public sealed class DataSink<T> : IDataSink, IDisposable
    where T : new()
{
    private bool _disposed;
    private readonly List<ErgoSolver> _solvers = new();

    private Channel<ITerm> Buffer;
    private Action<ITerm> DataPushedHandler;

    public event Action<ITerm> DataPushed;

    public readonly Atom Functor;

    public DataSink(Maybe<Atom> functor = default)
    {
        Functor = ErgoSolver.GetDataSignature<T>(functor).Functor;
        RegenerateBuffer();
    }

    public void Connect(ErgoSolver solver)
    {
        solver.DataPushed += OnDataPushed;
        _solvers.Add(solver);
    }

    public void Disconnect(ErgoSolver solver)
    {
        solver.DataPushed -= OnDataPushed;
        _solvers.Remove(solver);
    }

    private void OnDataPushed(ErgoSolver s, ITerm t)
    {
        if (t.GetFunctor().Select(some => some.Equals(Functor)).GetOr(t is Variable))
            DataPushed?.Invoke(t);
    }

    private void RegenerateBuffer()
    {
        Buffer = Channel.CreateUnbounded<ITerm>();
        DataPushedHandler = async t => await Buffer.Writer.WriteAsync(t);
        DataPushed += DataPushedHandler;
    }

    public async Task<T> PullOneAsync(CancellationToken ct = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DataSink<T>));

        var item = await Buffer.Reader.ReadAsync(ct);
        var ret = TermMarshall.FromTerm<T>(item);
        return ret;

    }

    public async IAsyncEnumerable<T> Pull([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DataSink<T>));

        if (DataPushedHandler != null)
        {
            DataPushed -= DataPushedHandler;
            DataPushedHandler = null;
        }

        if (!Buffer.Writer.TryComplete())
            throw new InvalidOperationException();

        await foreach (var item in Buffer.Reader.ReadAllAsync(ct))
        {
            yield return TermMarshall.FromTerm<T>(item);
        }

        RegenerateBuffer();
    }

    public void Dispose()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DataSink<T>));

        if (DataPushedHandler != null)
        {
            DataPushed -= DataPushedHandler;
            DataPushedHandler = null;
        }

        foreach (var solver in _solvers)
        {
            solver.DataPushed -= OnDataPushed;
        }

        _disposed = true;
    }

    IAsyncEnumerable<object> IDataSink.Pull(CancellationToken ct) => Pull(ct).Select(x => (object)x);
}
