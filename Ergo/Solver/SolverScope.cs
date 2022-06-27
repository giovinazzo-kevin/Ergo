using Ergo.Interpreter;
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
    public readonly InterpreterScope InterpreterScope;

    public SolverScope(InterpreterScope interp, int depth, Atom module, Maybe<Predicate> callee, ImmutableArray<Predicate> callers, CancellationTokenSource cut)
    {
        Depth = depth;
        Module = module;
        Callers = callers;
        Callee = callee;
        _cut = cut ?? new();
        InterpreterScope = interp;
    }

    public SolverScope WithModule(Atom module) => new(InterpreterScope, Depth, module, Callee, Callers, _cut);
    public SolverScope WithDepth(int depth) => new(InterpreterScope, depth, Module, Callee, Callers, _cut);
    public SolverScope WithCaller(Maybe<Predicate> caller) => new(InterpreterScope, Depth, Module, Callee, Callers.AddRange(caller.AsEnumerable()), _cut);
    public SolverScope WithCaller(Predicate caller) => new(InterpreterScope, Depth, Module, Callee, Callers.Add(caller), _cut);
    public SolverScope WithCallee(Maybe<Predicate> callee) => new(InterpreterScope, Depth, Module, callee, Callers, _cut);
    public SolverScope WithChoicePoint() => new(InterpreterScope, Depth, Module, Callee, Callers, cut: null);

    public void Throw(SolverError error, params object[] args) => InterpreterScope.ExceptionHandler.Throw(new SolverException(error, this, args));

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
        stackTrace = Callee
            .Select(some => stackTrace.Append($"[{depth - numCallers}] {some.Head.Explain(canonical: true)}"))
            .GetOr(stackTrace);
        return "\t" + stackTrace.Join("\r\n\t");

    }
}
