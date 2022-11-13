using Ergo.Events;
using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries;

// see https://github.com/G3Kappa/Ergo/issues/10
public abstract class Library
{
    public abstract Atom Module { get; }
    public abstract IEnumerable<InterpreterDirective> GetExportedDirectives();
    public abstract IEnumerable<SolverBuiltIn> GetExportedBuiltins();
    public virtual void OnErgoEvent(ErgoEvent evt) { }

}
