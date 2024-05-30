using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Runtime.BuiltIns;

public sealed class TermType : BuiltIn
{
    public TermType()
        : base("", "term_type", Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    private static readonly Atom _A = "atom";
    private static readonly Atom _V = "variable";
    private static readonly Atom _C = "complex";
    private static readonly Atom _B = "abstract";

    public override ErgoVM.Op Compile() => vm =>
    {
        var type = vm.Arg(0) switch
        {
            Atom => _A,
            Variable => _V,
            Complex => _C,
            AbstractTerm => _B,
            _ => throw new NotSupportedException()
        };
        vm.SetArg(0, vm.Arg(1));
        vm.SetArg(1, type);
        ErgoVM.Goals.Unify2(vm);
    };
}
