namespace Ergo.Runtime.BuiltIns;

public sealed class GetChar : ErgoBuiltIn
{
    public GetChar()
        : base("", new("get_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override Op Compile() => vm =>
    {
        int value;
        do
        {
            value = vm.In.Read();
        } while (value != '\n' && value != -1);
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");
        vm.SetArg(1, charTerm);
        ErgoVM.Goals.Unify2(vm);
    };
}