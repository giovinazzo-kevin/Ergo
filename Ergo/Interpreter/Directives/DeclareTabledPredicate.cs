using Ergo.Modules.Libraries.Tabling;

namespace Ergo.Modules.Directives;

public class DeclareTabledPredicate : ErgoDirective
{
    public DeclareTabledPredicate()
        : base("", new("table"), 1, 35)
    {
    }

    public override  bool Execute(ErgoModuleTree moduleTree, ImmutableArray<ITerm> args)
    {
        if (!Signature.FromCanonical(args[0], out var sig))
            sig = args[0].GetSignature();
        scope.GetLibrary<Tabling>(WellKnown.Modules.Tabling)
            .AddTabledPredicate(scope.Entry, sig);
        return true;
    }
}
