namespace Ergo.Lang
{
    public sealed class Eval1 : Evaluate
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
