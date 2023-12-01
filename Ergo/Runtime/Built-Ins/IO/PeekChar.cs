using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class PeekChar : BuiltIn
{
    public PeekChar()
        : base("", new("peek_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        int value = vm.In.Peek();
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");
        ErgoVM.Goals.Unify([args[0], charTerm]);
    };
}
