
using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;

public sealed class SequenceType : BuiltIn
{
    public SequenceType()
        : base("", new("seq_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    private static readonly Atom _L = new("list");
    private static readonly Atom _T = new("tuple");
    private static readonly Atom _S = new("set");

    public override ErgoVM.Goal Compile() => args =>
    {
        var (type, seq) = (args[1], args[0]);
        return vm =>
        {
            if (seq is Variable)
            {
                vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, seq.Explain());
                return;
            }
            if (seq is List)
            {
                ErgoVM.Goals.Unify([type, _L])(vm);
            }
            else if (seq is NTuple)
            {
                ErgoVM.Goals.Unify([type, _T])(vm);
            }
            else if (seq is Set)
            {
                ErgoVM.Goals.Unify([type, _S])(vm);
            }
        };
    };
}
