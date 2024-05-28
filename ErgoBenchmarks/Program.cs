using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Ergo.Lang.Ast;
using Ergo.Lang.Compiler;
using Ergo.Lang.Extensions;


var summary = BenchmarkRunner.Run<ImmutableTermsVsTermMemory>();

public class ImmutableTermsVsTermMemory
{
    const int ITER_COUNT = 5000000;

    public readonly TermMemory Memory = new();
    private ITermAddress PtrA, PtrB;


    [ParamsSource(nameof(ValuesForAB))]
    public (ITerm A, ITerm B) Params { get; set; }
    public IEnumerable<(ITerm, ITerm)> ValuesForAB => [
        (new Atom("test"), new Atom("test")),
        (new Variable("X"), new Variable("X")),
        (new Complex(new Atom("f"), new Variable("X")), new Complex(new Atom("f"), new Variable("Y"))),
        (new Dict(new Atom("f"), args: (KeyValuePair<Atom, ITerm>[])[new(new("a"), new Variable("X"))]),
         new Dict(new Atom("f"), args: (KeyValuePair<Atom, ITerm>[])[new(new("a"), new Variable("Y"))])),
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