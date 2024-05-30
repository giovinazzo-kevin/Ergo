namespace Ergo.Runtime.BuiltIns;

public sealed class PeekChar : BuiltIn
{
    public PeekChar()
        : base("", "peek_char", 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        int value = vm.In.Peek();
        ITerm charTerm = value != -1 ? (Atom)(char)value : "end_of_file";
        vm.SetArg(1, charTerm);
        ErgoVM.Goals.Unify2(vm);
    };
}
