using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ergo.Interpreter
{
    public sealed class DataSource : IAsyncEnumerable<ITerm>
    {
        private readonly Func<IAsyncEnumerable<ITerm>> Source;
        public event Action<DataSource, ITerm> ItemYielded;

        async IAsyncEnumerable<ITerm> FromEnumerable(IEnumerable<ITerm> data)
        {
            foreach (var item in data)
            {
                await Task.CompletedTask;
                yield return item;
                ItemYielded?.Invoke(this, item);
            }
        }

        async IAsyncEnumerable<ITerm> FromAsyncEnumerable(IAsyncEnumerable<ITerm> data)
        {
            await foreach (var item in data)
            {
                await Task.CompletedTask;
                yield return item;
                ItemYielded?.Invoke(this, item);
            }
        }

        public IAsyncEnumerator<ITerm> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return Source().GetAsyncEnumerator(cancellationToken);
        }

        public DataSource(IEnumerable<ITerm> source)
        {
            Source = () => FromEnumerable(source);
        }

        public DataSource(IAsyncEnumerable<ITerm> source)
        {
            Source = () => FromAsyncEnumerable(source);
        }
    }
}
