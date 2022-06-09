using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Solver;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ergo.Interpreter
{
    public sealed class DataSink<T> : IDisposable
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

        internal void Connect(ErgoSolver solver)
        {
            solver.DataPushed += OnDataPushed;
            _solvers.Add(solver);
        }

        internal void Disconnect(ErgoSolver solver)
        {
            solver.DataPushed -= OnDataPushed;
            _solvers.Remove(solver);
        }

        private void OnDataPushed(ErgoSolver s, ITerm t)
        {
            if (t.GetFunctor().Reduce(some => some.Equals(Functor), () => t is Variable))
            {
                DataPushed?.Invoke(t);
            }
        }

        private void RegenerateBuffer()
        {
            Buffer = Channel.CreateUnbounded<ITerm>();
            DataPushedHandler = async t => await Buffer.Writer.WriteAsync(t);
            DataPushed += DataPushedHandler;
        }

        public async IAsyncEnumerable<T> Pull([EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DataSink<T>));

            if (!Buffer.Writer.TryComplete())
            {
                throw new InvalidOperationException();
            }
            await foreach(var item in Buffer.Reader.ReadAllAsync(ct))
            {
                yield return TermMarshall.FromTerm<T>(item);
            }
            if (DataPushedHandler != null)
            {
                DataPushed -= DataPushedHandler;
                DataPushedHandler = null;
            }
            RegenerateBuffer();
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DataSink<T>));

            if(DataPushedHandler != null)
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
    }
}
