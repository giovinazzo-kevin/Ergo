using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class TermType : BuiltIn
{
    public TermType()
        : base("", new("term_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    private static readonly Atom _A = new("atom");
    private static readonly Atom _V = new("variable");
    private static readonly Atom _C = new("complex");
    private static readonly Atom _B = new("abstract");

    public override ErgoVM.Goal Compile() => args =>
    {
        var type = args[0] switch
        {
            Atom => _A,
            Variable => _V,
            Complex => _C,
            AbstractTerm => _B,
            _ => throw new NotSupportedException()
        };
        return ErgoVM.Goals.Unify([args[1], type]);
    };
}
