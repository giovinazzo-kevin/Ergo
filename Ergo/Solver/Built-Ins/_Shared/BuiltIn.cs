namespace Ergo.Solver.BuiltIns;

public abstract class SolverBuiltIn
{
    public readonly Signature Signature;
    public readonly string Documentation;

    public Predicate GetStub(ITerm[] arguments)
    {
        var head = new Complex(Signature.Functor, arguments);
        return new Predicate(Documentation, Signature.Module.Reduce(some => some, () => WellKnown.Modules.Stdlib), head, NTuple.Empty, dynamic: false, exported: true);
    }

    public abstract IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments);

    public SolverBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
    {
        Signature = new(functor, arity, Maybe.Some(module), Maybe<Atom>.None);
        Documentation = documentation;
    }
}
