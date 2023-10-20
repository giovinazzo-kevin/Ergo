
using Ergo.Interpreter.Libraries.Compiler;

namespace Ergo.Interpreter.Directives;

public class DeclareInlinedPredicate : InterpreterDirective
{
    public DeclareInlinedPredicate()
        : base("Marks a predicate to be inlined.", new("inline"), 1, 11)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        // TODO: make sure sig is unqualified, accept list of sigs
        if (args[0] is not List list)
        {
            scope.Throw(InterpreterError.ExpectedTermOfTypeAt, nameof(List), args[0].Explain());
            return false;
        }
        foreach (var arg in list.Contents)
        {
            if (!Signature.FromCanonical(arg, out var sig))
                sig = args[0].GetSignature();
            if (sig.Module.TryGetValue(out var module) && module != scope.Entry)
            {
                scope.Throw(InterpreterError.CantInlineForeignGoal, arg.Explain());
                return false;
            }
            sig = sig.WithModule(scope.Entry);
            scope.GetLibrary<Compiler>(WellKnown.Modules.Compiler)
                .AddInlinedPredicate(sig);
        }
        return true;
    }
}
