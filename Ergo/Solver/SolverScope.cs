using Ergo.Interpreter;
using System.Diagnostics;

namespace Ergo.Solver;

[DebuggerDisplay("{ Explain() }")]
public readonly struct SolverScope
{

    public readonly int Depth;
    public readonly Atom Module;
    public readonly ImmutableArray<Predicate> Callers;
    public readonly Predicate Callee;
    public readonly InterpreterScope InterpreterScope;
    public readonly bool IsCutRequested;
    public readonly InstantiationContext InstantiationContext;

    private SolverScope(InterpreterScope interp, int depth, Atom module, Predicate callee, ImmutableArray<Predicate> callers, bool cut, InstantiationContext ctx)
    {
        Depth = depth;
        Module = module;
        Callers = callers;
        Callee = callee;
        IsCutRequested = cut;
        InterpreterScope = interp;
        InstantiationContext = ctx;
    }
    public SolverScope(InterpreterScope interp, Atom module, InstantiationContext ctx)
        : this(interp, 0, module, default, ImmutableArray<Predicate>.Empty, false, ctx)
    {
    }

    public SolverScope WithModule(Atom module) => new(InterpreterScope, Depth, module, Callee, Callers, IsCutRequested, InstantiationContext);
    public SolverScope WithDepth(int depth) => new(InterpreterScope, depth, Module, Callee, Callers, IsCutRequested, InstantiationContext);
    public SolverScope WithCaller(Predicate caller) => new(InterpreterScope, Depth, Module, Callee, Callers.Add(caller), IsCutRequested, InstantiationContext);
    public SolverScope WithoutLastCaller() => new(InterpreterScope, Depth, Module, Callee, Callers.RemoveAt(Callers.Length - 1), IsCutRequested, InstantiationContext);
    public SolverScope WithCallee(Predicate callee) => new(InterpreterScope, Depth, Module, callee, Callers, IsCutRequested, InstantiationContext);
    public SolverScope WithChoicePoint() => new(InterpreterScope, Depth, Module, Callee, Callers, false, InstantiationContext);
    public SolverScope WithCut() => new(InterpreterScope, Depth, Module, Callee, Callers, true, InstantiationContext);
    public SolverScope WithInterpreterScope(InterpreterScope scope) => new(scope, Depth, Module, Callee, Callers, IsCutRequested, InstantiationContext);

    public void Throw(SolverError error, params object[] args) => InterpreterScope.ExceptionHandler.Throw(new SolverException(error, this, args));
    public bool ContainsCaller(Predicate caller) => Callers.Reverse().Any(c => c.IsSameDeclarationAs(caller));

    public string Explain()
    {
        var depth = Depth;
        var numCallers = Callers.Length;
        var stackTrace = Callers
            .Select((c, i) => $"[{depth - i}] {c.Head.Explain(canonical: true)}");
        stackTrace = stackTrace.Append($"[{depth - numCallers}] {Callee.Head.Explain(canonical: true)}");
        return "\t" + stackTrace.Join("\r\n\t");

    }
}
