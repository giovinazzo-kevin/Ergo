using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver
{
    public readonly struct SolverScope
    {
        public readonly int Depth;
        public readonly Atom Module;
        public readonly Maybe<Predicate> Caller;
        public readonly Maybe<Predicate> Callee;

        public SolverScope(int depth, Atom module, Maybe<Predicate> callee, Maybe<Predicate> caller)
        {
            Depth = depth;
            Module = module;
            Caller = caller;
            Callee = callee;
        }
    }
}
