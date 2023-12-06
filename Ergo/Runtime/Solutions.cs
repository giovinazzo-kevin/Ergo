using System.Collections;

namespace Ergo.Runtime;

public sealed class Solutions : IEnumerable<Solution>
{
    public delegate IReadOnlyList<Solution> Generator(int num);

    class SingleEnumerable(Solution sol) : IReadOnlyList<Solution>
    {
        public Solution this[int index] => index == 0 ? sol : throw new ArgumentOutOfRangeException(nameof(index));
        public int Count => 1;

        public IEnumerator<Solution> GetEnumerator()
        {
            yield return sol;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class GeneratorDef(int num, IReadOnlyList<Solution> sol)
    {
        public int NumSolutions { get; set; } = num;
        public IReadOnlyList<Solution> Solutions { get; set; } = sol;
    }

    private readonly List<GeneratorDef> generators = new();
    public int Count { get; private set; }

    public void Clear()
    {
        generators.Clear();
        Count = 0;
    }

    public GeneratorDef Push(Generator gen, int num)
    {
        if (num <= 0)
            return default;
        Count += num;
        var def = new GeneratorDef(num, gen(num));
        generators.Add(def);
        return def;
    }

    public GeneratorDef Push(SubstitutionMap subs)
    {
        return Push(_ => new SingleEnumerable(new(subs)), 1);
    }

    public Maybe<Solution> Pop()
    {
        if (Count == 0)
            return default;
        Count--;
        var gc = generators.Count - 1;
        var gen = generators[gc];
        var sols = gen.Solutions;
        if (gen.NumSolutions == 1)
            generators.RemoveAt(gc);
        return sols[sols.Count - gen.NumSolutions--];

    }

    public IEnumerator<Solution> GetEnumerator()
    {
        return generators
            .SelectMany(gen => gen.Solutions.Take(gen.NumSolutions))
            .GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
