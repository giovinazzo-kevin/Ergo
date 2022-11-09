using Ergo.Interpreter.Directives;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries;

// see https://github.com/G3Kappa/Ergo/issues/10
public abstract class Library
{
    public abstract Atom Module { get; }
    public abstract IEnumerable<InterpreterDirective> GetExportedDirectives();
    public abstract IEnumerable<SolverBuiltIn> GetExportedBuiltins();
    protected virtual void OnLoaded(ErgoSolver solver, ref SolverScope scope)
    {

    }
    internal void Load(ErgoSolver solver, ref SolverScope scope)
    {
        OnLoaded(solver, ref scope);
    }
}
