using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class SequenceType : BuiltIn
{
    public SequenceType()
        : base("", new("seq_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    private static readonly Atom _L = new("list");
    private static readonly Atom _T = new("tuple");
    private static readonly Atom _S = new("set");

    public override ErgoVM.Op Compile() => vm =>
    {
        var (L, T, S) = (vm.Memory.StoreAtom(_L), vm.Memory.StoreAtom(_T), vm.Memory.StoreAtom(_S));
        var (type, seq) = (vm.Arg2(2), vm.Arg2(1));
        if (seq is VariableAddress)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, seq.Deref(vm).Explain());
            return;
        }
        vm.SetArg2(1, type);
        if (seq is AbstractAddress a)
        {
            var t = vm.Memory[a].Type;
            if (t == typeof(List))
                vm.SetArg2(2, L);
            else if (t == typeof(NTuple))
                vm.SetArg2(2, T);
            else if (t == typeof(Set))
                vm.SetArg2(2, S);
        }
        else if (seq is ConstAddress c)
        {
            var f = vm.Memory[c];
            if (f.Equals(WellKnown.Literals.EmptyList))
                vm.SetArg2(2, L);
            else if (f.Equals(WellKnown.Literals.EmptyCommaList))
                vm.SetArg2(2, T);
            else if (f.Equals(WellKnown.Literals.EmptySet))
                vm.SetArg2(2, S);
        }
        ErgoVM.Goals.Unify2(vm);
    };
}
