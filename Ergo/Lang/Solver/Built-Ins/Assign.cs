namespace Ergo.Lang.BuiltIns
{
    public sealed class Assign : BuiltIn
    {
        public Assign()
            : base("", new("@assign"), Maybe<int>.Some(2))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            return new(Literals.True, new Substitution(arguments[0], arguments[1]));
        }
    }
}
