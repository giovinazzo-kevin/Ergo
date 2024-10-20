namespace Ergo.Modules.Directives;

public class DeclareDynamicPredicate() : ErgoDirective("", new("dynamic"), 1, 30)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        var pTable = ctx.CurrentModule.MetaPredicateTable;
        if (!pTable.TryGetValue(sig, out var pInfo))
            pTable[sig] = pInfo = new();
        pTable[sig] = pInfo with { IsDynamic = true };
        return true;
    }
}
