using System.Diagnostics;

namespace Ergo.Solver;

[DebuggerDisplay("{ Explain() }")]
public readonly struct SolverScope
{
    private readonly CancellationTokenSource _cut;

    public readonly int Depth;
    public readonly Atom Module;
    public readonly ImmutableArray<Predicate> Callers;
    public readonly Maybe<Predicate> Callee;

    public SolverScope(int depth, Atom module, Maybe<Predicate> callee, ImmutableArray<Predicate> callers, CancellationTokenSource cut = null)
    {
        Depth = depth;
        Module = module;
        Callers = callers;
        Callee = callee;
        _cut = cut ?? new();
    }

    public SolverScope WithModule(Atom module) => new(Depth, module, Callee, Callers);
    public SolverScope WithDepth(int depth) => new(depth, Module, Callee, Callers);
    public SolverScope WithCaller(Maybe<Predicate> caller)
    {
        var _callers = Callers;
        return new(Depth, Module, Callee, caller.Reduce(some => _callers.Add(some), () => _callers));
    }
    public SolverScope WithCaller(Predicate caller) => new(Depth, Module, Callee, Callers.Add(caller));
    public SolverScope WithCallee(Maybe<Predicate> callee) => new(Depth, Module, callee, Callers);
    public SolverScope WithChoicePoint() => new(Depth, Module, Callee, Callers, cut: null);

    public bool IsCutRequested => _cut.IsCancellationRequested;
    public bool Cut()
    {
        if (IsCutRequested)
            return false;
        _cut.Cancel();
        return true;
    }

    public string Explain()
    {
        var depth = Depth;
        var numCallers = Callers.Length;
        var stackTrace = Callers
            .Select((c, i) => $"[{depth - i}] {c.Head.Explain(canonical: true)}");
        stackTrace = Callee.Reduce(some => stackTrace.Append($"[{depth - numCallers}] {some.Head.Explain(canonical: true)}"), () => stackTrace);
        return "\t" + string.Join("\r\n\t", stackTrace);

    }
}
