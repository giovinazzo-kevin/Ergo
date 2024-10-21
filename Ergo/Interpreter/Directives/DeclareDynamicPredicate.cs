namespace Ergo.Modules.Directives;

public class DeclareDynamicPredicate() : ErgoDirective("", new("dynamic"), 1, 30)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        var pInfo = ctx.CurrentModule.GetMetaTableEntry(sig);
        ctx.CurrentModule.SetMetaTableEntry(sig, pInfo with { IsDynamic = true });
        return true;
    }
}
