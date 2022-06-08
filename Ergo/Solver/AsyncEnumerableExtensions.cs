using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ergo.Solver
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> CollectAsync<T>(this IAsyncEnumerable<T> solutions)
        {
            var bag = new List<T>();
            await foreach (var item in solutions)
            {
                bag.Add(item);
            }
            return bag;
        }
    }
}
