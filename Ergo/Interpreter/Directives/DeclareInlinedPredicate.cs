namespace Ergo.Modules.Directives;

public class DeclareInlinedPredicate() : ErgoDirective("Marks a predicate to be inlined.", new("inline"), default, 11)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        foreach (var arg in args)
        {
            if (!Signature.FromCanonical(arg, out var sig))
                sig = args[0].GetSignature();
            if (sig.Module.TryGetValue(out var module) && module != ctx.CurrentModule.Name)
                throw new InterpreterException(ErgoInterpreter.ErrorType.CantInlineForeignGoal, arg.Explain());
            sig = sig.WithModule(ctx.CurrentModule.Name);
            ctx.ModuleTree.GetLibrary<Libraries.Compiler.Compiler>()
                .InlinedPredicates.Add(sig);
        }
        return true;
    }
}
