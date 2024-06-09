namespace Ergo.Runtime.BuiltIns;

public sealed class AnonymousComplex : BuiltIn
{
    public AnonymousComplex()
        : base("", "anon", Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (!vm.Arg(1).Match<int>(out var arity))
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, vm.Arg(1).Explain());
            return;
        }
        if (vm.Arg(0) is not Atom functor)
        {
            if (vm.Arg(0).GetQualification(out var qs).TryGetValue(out var qm) && qs is Atom functor_)
            {
                var cplx = functor_.BuildAnonymousTerm(arity)
                    .Qualified(qm);
                vm.SetArg(0, cplx);
                vm.SetArg(1, vm.Arg(2));
                ErgoVM.Goals.Unify2(vm);
            }

            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Functor, vm.Arg(0).Explain());
            return;
        }
        var anon = functor.BuildAnonymousTerm(arity)
            .Qualified(vm.CKB.Scope.Entry);
        vm.SetArg(0, anon);
        vm.SetArg(1, vm.Arg(2));
        ErgoVM.Goals.Unify2(vm);
    };
}
