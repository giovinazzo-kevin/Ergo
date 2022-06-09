using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Solver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ergo.Interpreter
{
    public sealed class DataSource<T> : IAsyncEnumerable<ITerm>
        where T : new()
    {
        public readonly Atom Functor;
        public readonly DataSource Source;
        public event Action<DataSource<T>, T, ITerm> ItemYielded;

        async IAsyncEnumerable<ITerm> FromEnumerable(Func<IEnumerable<T>> data)
        {
            foreach (var item in data())
            {
                await Task.CompletedTask;
                var term = TermMarshall.ToTerm(item);
                yield return term;
                ItemYielded?.Invoke(this, item, term);
            }
        }

        async IAsyncEnumerable<ITerm> FromAsyncEnumerable(Func<IAsyncEnumerable<T>> data)
        {
            await foreach (var item in data())
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

            public DataSource(Func<IEnumerable<T>> source, Maybe<Atom> functor = default)
        {
            Functor = ErgoSolver.GetDataSignature<T>(functor).Functor;
            Source = new(() => FromEnumerable(source));
        }

        public DataSource(Func<IAsyncEnumerable<T>> source, Maybe<Atom> functor = default)
        {
            Functor = ErgoSolver.GetDataSignature<T>(functor).Functor;
            Source = new(() => FromAsyncEnumerable(source));
        }
    }
}
