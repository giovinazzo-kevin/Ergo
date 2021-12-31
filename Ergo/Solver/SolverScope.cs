using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Solver
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct SolverScope
    {
        public readonly int Depth;
        public readonly Atom Module;
        public readonly ImmutableArray<Predicate> Callers;
        public readonly Maybe<Predicate> Callee;

        public SolverScope(int depth, Atom module, Maybe<Predicate> callee, ImmutableArray<Predicate> callers)
        {
            Depth = depth;
            Module = module;
            Callers = callers;
            Callee = callee;
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

        public string Explain()
        {
            var depth = Depth;
            var numCallers = Callers.Length;
            var stackTrace = Callers
                .Select((c, i) => $"[{depth - i}] {c.Head.Explain()}");
            stackTrace = Callee.Reduce(some => stackTrace.Append($"[{depth - numCallers}] {some.Head.Explain()}"), () => stackTrace);
            return "\t" + string.Join("\r\n\t", stackTrace);

        }
    }
}
