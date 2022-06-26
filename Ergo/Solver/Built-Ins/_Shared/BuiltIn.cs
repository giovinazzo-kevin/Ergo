namespace Ergo.Solver.BuiltIns;

public abstract class SolverBuiltIn
{
    public readonly Signature Signature;
    public readonly string Documentation;

    public Predicate GetStub(ITerm[] arguments)
    {
        var head = new Complex(Signature.Functor, arguments);
        return new Predicate(Documentation, Signature.Module.GetOr(WellKnown.Modules.Stdlib), head, NTuple.Empty, dynamic: false, exported: true);
    }

    protected Evaluation ThrowFalse(SolverScope scope, SolverError error, params object[] args)
    {
        scope.InterpreterScope.ExceptionHandler.Throw(new SolverException(error, scope, args));
        return False();
    }
    protected Evaluation False() => new(WellKnown.Literals.False);
    protected Evaluation True(IEnumerable<Substitution> subs) => new(WellKnown.Literals.True, subs.ToArray());
    protected Evaluation True(params Substitution[] subs) => new(WellKnown.Literals.True, subs);

    public abstract IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments);

    public SolverBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
    {
        Signature = new(functor, arity, module, Maybe<Atom>.None);
        Documentation = documentation;
    }
}
