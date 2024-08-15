
using Ergo.Interpreter.Libraries.Compiler;

namespace Ergo.Interpreter.Directives;

public class DeclareInlinedPredicate : InterpreterDirective
{
    public DeclareInlinedPredicate()
        : base("Marks a predicate to be inlined.", new("inline"), default, 11)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        foreach (var arg in args)
        {
            if (!Signature.FromCanonical(arg, out var sig))
                sig = args[0].GetSignature();
            if (sig.Module.TryGetValue(out var module) && module != scope.Entry)
            {
                scope.Throw(ErgoInterpreter.ErrorType.CantInlineForeignGoal, arg.Explain());
                return false;
            }
            sig = sig.WithModule(scope.Entry);
            scope.GetLibrary<Compiler>(WellKnown.Modules.Compiler)
                .AddInlinedPredicate(sig);
        }
        return true;
    }
}
