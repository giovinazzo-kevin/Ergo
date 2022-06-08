using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ergo.Interpreter
{
    public sealed class DataSource<T> : IAsyncEnumerable<ITerm>
    {
        public readonly DataSource Source;
        public event Action<DataSource<T>, T, ITerm> ItemYielded;

        async IAsyncEnumerable<ITerm> FromEnumerable(IEnumerable<T> data)
        {
            foreach (var item in data)
            {
                await Task.CompletedTask;
                var term = TermMarshall.ToTerm(item);
                yield return term;
                ItemYielded?.Invoke(this, item, term);
            }
        }

        async IAsyncEnumerable<ITerm> FromAsyncEnumerable(IAsyncEnumerable<T> data)
        {
            await foreach (var item in data)
            {
                await Task.CompletedTask;
                var term = TermMarshall.ToTerm(item);
                yield return term;
                ItemYielded?.Invoke(this, item, term);
            }
        }

        public IAsyncEnumerator<ITerm> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return ((IAsyncEnumerable<ITerm>)Source).GetAsyncEnumerator(cancellationToken);
        }

        public DataSource(IEnumerable<T> source)
        {
            Source = new(FromEnumerable(source));
        }

        public DataSource(IAsyncEnumerable<T> source)
        {
            Source = new(FromAsyncEnumerable(source));
        }
    }
}
