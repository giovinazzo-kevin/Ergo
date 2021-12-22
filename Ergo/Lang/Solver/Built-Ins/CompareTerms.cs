namespace Ergo.Lang.BuiltIns
{
    public sealed class CompareTerms : BuiltIn
    {
        public CompareTerms()
            : base("", new("@compare"), Maybe<int>.Some(3))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            var cmp = (double)arguments[1].CompareTo(arguments[2]);
            if (arguments[0].IsGround)
            {
                if (!arguments[0].Matches<int>(out var result))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, arguments[0].Explain());
                }
                if (result.Equals(cmp))
                {
                    return new(Literals.True);
                }
                return new(Literals.False);
            }
            return new(Literals.True, new Substitution(arguments[0], new Atom(cmp)));
        }
    }
}
