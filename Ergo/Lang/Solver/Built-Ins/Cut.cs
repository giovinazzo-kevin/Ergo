using Ergo.Lang.Ast;

namespace Ergo.Lang.BuiltIns
{
    public sealed class Cut : BuiltIn
    {
        public Cut()
            : base("", new("@cut"), Maybe<int>.Some(0))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            return new(Literals.True);
        }
    }
}
