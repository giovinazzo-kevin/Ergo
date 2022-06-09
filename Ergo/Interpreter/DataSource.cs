using Ergo.Lang.Ast;
using Ergo.Solver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ergo.Interpreter
{

    public sealed class DataSource : IAsyncEnumerable<ITerm>
    {
        private readonly Queue<ITerm> Rejects = new();
        private readonly Func<IAsyncEnumerable<ITerm>> Source;
        public event Action<DataSource, ITerm> ItemYielded;

        internal void Recycle(ITerm term) => Rejects.Enqueue(term);

        async IAsyncEnumerable<ITerm> FromEnumerable(Func<IEnumerable<ITerm>> data)
        {
            while (Rejects.TryDequeue(out var recycled))
                yield return recycled;
            foreach (var item in data())
            {
                while (Rejects.TryDequeue(out var recycled))
                    yield return recycled;
                await Task.CompletedTask;
                yield return item;
                ItemYielded?.Invoke(this, item);
            }
        }

        async IAsyncEnumerable<ITerm> FromAsyncEnumerable(Func<IAsyncEnumerable<ITerm>> data)
        {
            while (Rejects.TryDequeue(out var recycled))
                yield return recycled;
            await foreach (var item in data())
            {
                while (Rejects.TryDequeue(out var recycled))
                    yield return recycled;
                yield return item;
                ItemYielded?.Invoke(this, item);
            }
        }

        public IAsyncEnumerator<ITerm> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return Source().GetAsyncEnumerator(cancellationToken);
        }

        public DataSource(Func<IEnumerable<ITerm>> source)
        {
            Source = () => FromEnumerable(source);
        }

        public DataSource(Func<IAsyncEnumerable<ITerm>> source)
        {
            Source = () => FromAsyncEnumerable(source);
        }
    }
}
