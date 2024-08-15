using Ergo.Events;
using Ergo.Interpreter.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter.Libraries;

// see https://github.com/G3Kappa/Ergo/issues/10
public abstract class Library
{
    public virtual int LoadOrder => 0;
    public abstract Atom Module { get; }
    public abstract IEnumerable<InterpreterDirective> ExportedDirectives { get; }
    public abstract IEnumerable<BuiltIn> ExportedBuiltins { get; }
    public virtual void OnErgoEvent(ErgoEvent evt) { }

}
