using System.Linq;

namespace Ergo.Lang
{
    public sealed class Not : BuiltIn
    {
        public Not()
            : base("", new("@not"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            var arg = arguments.Single();
            if (!arg.Matches<bool>(out var eval))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Boolean, arg.Explain());
            }
            return new(new Atom(!eval));
        }
    }
}
