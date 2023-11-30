using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;
public sealed class GetSingleChar : SolverBuiltIn
{
    public GetSingleChar()
        : base("", new("get_single_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override ErgoVM.Goal Compile() => arguments => vm =>
    {
        int value = vm.In.Read();
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");
        ErgoVM.Goals.Unify([arguments[0], charTerm])(vm);
    };
}
