using System.Text;

namespace Ergo.Runtime.BuiltIns;

public sealed class ReadLine : BuiltIn
{
    public ReadLine()
        : base("", "read_line", 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        int value;
        var builder = new StringBuilder();
        while ((value = vm.In.Read()) != -1 && value != '\n')
        {
            builder.Append((char)value);
        }
        Atom lineTerm = value != -1 ? builder.ToString() : "end_of_file";
        vm.SetArg(1, lineTerm);
        ErgoVM.Goals.Unify2(vm);
    };
}