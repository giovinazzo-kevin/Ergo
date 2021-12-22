using Ergo.Lang.Ast;

namespace Ergo.Lang.BuiltIns
{
    public sealed class Eval1 : MathBuiltIn
    {
        public Eval1()
            : base("", new("@eval"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            return new(new Atom(Eval(arguments[0])));
        }
    }
}
