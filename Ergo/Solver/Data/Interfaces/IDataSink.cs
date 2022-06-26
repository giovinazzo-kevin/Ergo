namespace Ergo.Solver.DataBindings.Interfaces;

public interface IDataSink
{
    void Connect(ErgoSolver solver);
    void Disconnect(ErgoSolver solver);
    IAsyncEnumerable<object> Pull(CancellationToken ct = default);
}
