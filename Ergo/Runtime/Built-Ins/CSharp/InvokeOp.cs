namespace Ergo.Runtime.BuiltIns;

public sealed class InvokeOp : ErgoBuiltIn
{
    public InvokeOp()
        : base("", new("invoke"), Maybe<int>.Some(1), WellKnown.Modules.CSharp)
    {
    }

    public override Op Compile() => vm =>
    {
        if (vm.Arg(0) is not Atom { Value: Op p })
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, typeof(Clause), vm.Arg(0).Explain());
            return;
        }
        p(vm);
    };
}
