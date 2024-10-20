using Ergo.Modules.Libraries.Tabling;

namespace Ergo.Modules.Directives;

public class DeclareTabledPredicate() : ErgoDirective("", new("table"), 1, 35)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        ctx.ModuleTree.GetLibrary<Tabling>()
            .AddTabledPredicate(ctx.CurrentModule.Name, sig);
        return true;
    }
}
