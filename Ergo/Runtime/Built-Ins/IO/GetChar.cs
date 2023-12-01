using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class GetChar : BuiltIn
{
    public GetChar()
        : base("", new("get_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Goal Compile() => arguments => vm =>
    {
        int value;
        do
        {
            value = vm.In.Read();
        } while (value != '\n' && value != -1);
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");
        ErgoVM.Goals.Unify([arguments[0], charTerm])(vm);
    };
}