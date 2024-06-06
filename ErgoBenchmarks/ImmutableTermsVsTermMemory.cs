using BenchmarkDotNet.Attributes;
using Ergo.Lang.Ast;
using Ergo.Lang.Compiler;
using Ergo.Lang.Extensions;

namespace Ergo.Benchmarks;

public class ImmutableTermsVsTermMemory
{
    const int ITER_COUNT = 1000000;

    public readonly TermMemory Memory = new();
    private ITermAddress PtrA = null!, PtrB = null!;


    [ParamsSource(nameof(ValuesForAB))]
    public (ITerm A, ITerm B) Params { get; set; }
    public static IEnumerable<(ITerm, ITerm)> ValuesForAB => [
        ((Atom)"test", (Atom)"test"),
        ((Variable)"X", (Variable)"X"),
        (new Complex("f", (Variable)"X"), new Complex("f", (Variable)"Y")),
        (new Dict((Atom)"f", args: (KeyValuePair<Atom, ITerm>[])[new("a", (Variable)"X")]),
         new Dict((Atom)"f", args: (KeyValuePair<Atom, ITerm>[])[new("a", (Variable)"Y")])),
    ];

    [Benchmark]
    public bool UnifyImmutable()
    {
        for (int i = 0; i < ITER_COUNT; i++)
        {
            if (!Params.A.Unify(Params.B).TryGetValue(out _))
                throw new InvalidOperationException();
        }
        return true;
    }

    [Benchmark]
    public bool UnifyTermMemory()
    {
        for (int i = 0; i < ITER_COUNT; i++)
        {
            if (!Memory.Unify(PtrA, PtrB, transaction: false))
                throw new InvalidOperationException();
        }
        return true;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        PtrA = Memory.StoreTerm(Params.A);
        PtrB = Memory.StoreTerm(Params.B);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        Memory.Clear();
    }
}