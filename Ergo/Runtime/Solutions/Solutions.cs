using System.Collections;

namespace Ergo.Runtime;

/// <summary>
/// Specialized class that allows deferring the generation of solutions until they are enumerated.
/// </summary>
public sealed class Solutions : IEnumerable<Solution>
{
    public delegate ISolutionEnumerable Generator(int num);

    public class GeneratorDef(int num, ISolutionEnumerable sol)
    {
        public int NumSolutions { get; set; } = num;
        public ISolutionEnumerable Solutions { get; set; } = sol;
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
        return Push(_ => new SingleSolutionEnumerable(new(subs)), 1);
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
        return Inner()
            .GetEnumerator();
        IEnumerable<Solution> Inner()
        {
            foreach (var sol in generators
            .SelectMany(gen => gen.Solutions.Take(gen.NumSolutions)))
                yield return sol;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
