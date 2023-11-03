using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public abstract class SolverBuiltIn
{
    public readonly Signature Signature;
    public readonly string Documentation;

    public virtual int OptimizationOrder => 0;

    public virtual Maybe<ExecutionNode> Optimize(BuiltInNode node) => default;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;

    public Predicate GetStub(ImmutableArray<ITerm> arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Predicate(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true, default);
    }

    protected Evaluation ThrowFalse(SolverScope scope, SolverError error, params object[] args)
    {
        scope.InterpreterScope.ExceptionHandler.Throw(new SolverException(error, scope, args));
        return False();
    }
    protected Evaluation Bool(bool b) => new(b);
    protected Evaluation False() => new(false);
    protected Evaluation True() => new(true);
    protected Evaluation True(SubstitutionMap subs) => new(true, subs);
    protected Evaluation True(Substitution sub) => new(true, sub);

    public abstract IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ImmutableArray<ITerm> arguments);

    public SolverBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
    {
        Signature = new(functor, arity, module, Maybe<Atom>.None);
        Documentation = documentation;
    }
}
