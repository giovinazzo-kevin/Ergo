using Ergo.Interpreter;
using System.Diagnostics;

namespace Ergo.Solver;

[DebuggerDisplay("{ Explain() }")]
public readonly struct SolverScope
{

    public readonly int Depth;
    public readonly Atom Module;
    public readonly ImmutableArray<Predicate> Callers;
    public readonly Maybe<Predicate> Callee;
    public readonly InterpreterScope InterpreterScope;
    public readonly bool IsCutRequested;

    public SolverScope(InterpreterScope interp, int depth, Atom module, Maybe<Predicate> callee, ImmutableArray<Predicate> callers, bool cut)
    {
        Depth = depth;
        Module = module;
        Callers = callers;
        Callee = callee;
        IsCutRequested = cut;
        InterpreterScope = interp;
    }

    public SolverScope WithModule(Atom module) => new(InterpreterScope, Depth, module, Callee, Callers, IsCutRequested);
    public SolverScope WithDepth(int depth) => new(InterpreterScope, depth, Module, Callee, Callers, IsCutRequested);
    public SolverScope WithCaller(Maybe<Predicate> caller) => new(InterpreterScope, Depth, Module, Callee, Callers.AddRange(caller.AsEnumerable()), IsCutRequested);
    public SolverScope WithCaller(Predicate caller) => new(InterpreterScope, Depth, Module, Callee, Callers.Add(caller), IsCutRequested);
    public SolverScope WithCallee(Maybe<Predicate> callee) => new(InterpreterScope, Depth, Module, callee, Callers, IsCutRequested);
    public SolverScope WithChoicePoint() => new(InterpreterScope, Depth, Module, Callee, Callers, cut: false);
    public SolverScope WithCut() => new(InterpreterScope, Depth, Module, Callee, Callers, cut: true);
    public SolverScope WithoutCut() => new(InterpreterScope, Depth, Module, Callee, Callers, cut: false);

    public void Throw(SolverError error, params object[] args) => InterpreterScope.ExceptionHandler.Throw(new SolverException(error, this, args));

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
