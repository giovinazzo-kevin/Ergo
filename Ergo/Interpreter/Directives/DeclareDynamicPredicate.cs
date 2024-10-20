namespace Ergo.Modules.Directives;

public class DeclareDynamicPredicate : ErgoDirective
{
    public DeclareDynamicPredicate()
        : base("", new("dynamic"), 1, 30)
    {
    }

    public override bool Execute(ErgoModuleTree moduleTree, ImmutableArray<ITerm> args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
        {
            sig = args[0].GetSignature();
        }

        scope = scope.WithModule(scope.EntryModule
            .WithDynamicPredicate(sig));
        return true;
    }
}
