using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;

public sealed class AnonymousComplex : BuiltIn
{
    public AnonymousComplex()
        : base("", new("anon"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
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
                ErgoVM.Goals.Unify([cplx, args[2]]);
            }

            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Functor, args[0].Explain());
            return;
        }
        var anon = functor.BuildAnonymousTerm(arity)
            .Qualified(vm.KnowledgeBase.Scope.Entry);
        ErgoVM.Goals.Unify([anon, args[2]]);
    };
}
