namespace Ergo.Solver.BuiltIns;

public abstract class SolverBuiltIn
{
    public readonly Signature Signature;
    public readonly string Documentation;

    public Predicate GetStub(ITerm[] arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Predicate(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true);
    }

    protected Evaluation ThrowFalse(SolverScope scope, SolverError error, params object[] args)
    {
        scope.InterpreterScope.ExceptionHandler.Throw(new SolverException(error, scope, args));
        return False();
    }
    protected Evaluation Bool(bool b) => b ? True() : False();
    protected Evaluation False() => new(WellKnown.Literals.False);
    protected Evaluation True() => new(WellKnown.Literals.True);
    protected Evaluation True(SubstitutionMap subs) => new(WellKnown.Literals.True, subs);
    protected Evaluation True(Substitution sub) => new(WellKnown.Literals.True, sub);

    public abstract IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ITerm[] arguments);

    public SolverBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
    {
        Signature = new(functor, arity, module, Maybe<Atom>.None);
        Documentation = documentation;
    }
}
