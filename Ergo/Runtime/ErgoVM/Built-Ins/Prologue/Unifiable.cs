namespace Ergo.Runtime.BuiltIns;

public sealed class Unifiable : BuiltIn
{
    public Unifiable()
        : base("", new("unifiable"), Maybe<int>.Some(3), WellKnown.Modules.Prologue)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0].Unify(args[1]).TryGetValue(out var subs))
        {
            var equations = subs.Select(s => (ITerm)new Complex(WellKnown.Operators.Unification.CanonicalFunctor, s.Lhs, s.Rhs)
                .AsOperator(WellKnown.Operators.Unification));
            List list = new(ImmutableArray.CreateRange(equations), default, default);
            vm.SetArg(0, args[2]);
            vm.SetArg(1, list);
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();
    };
}
