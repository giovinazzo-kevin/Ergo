using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo.Solver
{
    public readonly struct SolverScope
    {
        public readonly Atom Module;
        public readonly Maybe<Predicate> Caller;
        public readonly Maybe<Predicate> Callee;

        public SolverScope(Atom module, Maybe<Predicate> callee, Maybe<Predicate> caller)
        {
            Module = module;
            Caller = caller;
            Callee = callee;
        }
    }
}
