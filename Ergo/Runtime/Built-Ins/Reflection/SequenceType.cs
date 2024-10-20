namespace Ergo.Runtime.BuiltIns;

public sealed class SequenceType : ErgoBuiltIn
{
    public SequenceType()
        : base("", new("seq_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    private static readonly Atom _L = new("list");
    private static readonly Atom _T = new("tuple");
    private static readonly Atom _S = new("set");

    public override Op Compile() => vm =>
    {
        var args = vm.Args;
        var (type, seq) = (args[1], args[0]);
        if (seq is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, seq.Explain());
            return;
        }
        vm.SetArg(0, type);
        if (seq is List)
        {
            vm.SetArg(1, _L);
            ErgoVM.Goals.Unify2(vm);
        }
        else if (seq is NTuple)
        {
            vm.SetArg(1, _T);
            ErgoVM.Goals.Unify2(vm);
        }
        else if (seq is Set)
        {
            vm.SetArg(1, _S);
            ErgoVM.Goals.Unify2(vm);
        }
    };
}
