using Ergo.Lang.Compiler;
using System.Text;

namespace Ergo.Solver.BuiltIns;

public sealed class ReadLine : SolverBuiltIn
{
    public ReadLine()
        : base("", new("read_line"), 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        int value;
        var builder = new StringBuilder();
        while ((value = vm.In.Read()) != -1 && value != '\n')
        {
            builder.Append((char)value);
        }
        ITerm lineTerm = value != -1 ? new Atom(builder.ToString()) : new Atom("end_of_file");
        ErgoVM.Goals.Unify([args[0], lineTerm])(vm);
    };
}