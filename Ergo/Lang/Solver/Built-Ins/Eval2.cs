using System.Linq;

namespace Ergo.Lang.BuiltIns
{
    public sealed class Eval2 : MathBuiltIn
    {
        public Eval2()
            : base("", new("@eval"), Maybe<int>.Some(2))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            var result = new Atom(Eval(arguments[1]));
            if (new Substitution(arguments[0], result).TryUnify(out var subs)) {
                return new(Literals.True, subs.ToArray());
            }
            return new(Literals.False);
        }
    }
}
