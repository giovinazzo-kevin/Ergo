namespace Ergo.Runtime.BuiltIns;

public sealed class PeekChar : ErgoBuiltIn
{
    public PeekChar()
        : base("", new("peek_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override Op Compile() => vm =>
    {
        int value = vm.In.Peek();
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");
        vm.SetArg(1, charTerm);
        ErgoVM.Goals.Unify2(vm);
    };
}
