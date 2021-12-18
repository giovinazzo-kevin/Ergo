namespace Ergo.Lang
{

    public partial class Solver
    {
        public readonly struct Scope
        {
            public readonly Atom Module;
            public readonly Maybe<Predicate> Caller;
            public readonly Maybe<Predicate> Callee;

            public Scope(Atom module, Maybe<Predicate> callee, Maybe<Predicate> caller)
            {
                Module = module;
                Caller = caller;
                Callee = callee;
            }
        }
    }
}
