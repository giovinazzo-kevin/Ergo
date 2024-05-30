namespace Ergo.Runtime.BuiltIns;

public sealed class InvokeOp : BuiltIn
{
    public InvokeOp()
        : base("", "invoke", Maybe<int>.Some(1), WellKnown.Modules.CSharp)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Arg(0) is not Atom { Value: ErgoVM.Op p })
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(Predicate), vm.Arg(0).Explain());
            return;
        }
        p(vm);
    };
}
