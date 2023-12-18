namespace Ergo.Runtime.BuiltIns;

public sealed class AnonymousComplex : BuiltIn
{
    public AnonymousComplex()
        : base("", new("anon"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (!args[1].Matches<int>(out var arity))
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1].Explain());
            return;
        }
        if (args[0] is not Atom functor)
        {
            if (args[0].GetQualification(out var qs).TryGetValue(out var qm) && qs is Atom functor_)
            {
                var cplx = functor_.BuildAnonymousTerm(arity)
                    .Qualified(qm);
                vm.SetArg(0, cplx);
                vm.SetArg(1, args[2]);
                ErgoVM.Goals.Unify2(vm);
            }

            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Functor, args[0].Explain());
            return;
        }
        var anon = functor.BuildAnonymousTerm(arity)
            .Qualified(vm.KnowledgeBase.Scope.Entry);
        vm.SetArg(0, anon);
        vm.SetArg(1, args[2]);
        ErgoVM.Goals.Unify2(vm);
    };
}
