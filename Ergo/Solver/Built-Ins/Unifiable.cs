using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Unifiable : BuiltIn
    {
        public Unifiable()
            : base("", new("@unifiable"), Maybe<int>.Some(3))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (new Substitution(arguments[0], arguments[1]).TryUnify(out var subs)) {
                var equations = subs.Select(s => (ITerm)new Complex(Operators.BinaryUnification.CanonicalFunctor, s.Lhs, s.Rhs));
                var list = new List(ImmutableArray.CreateRange(equations));
                if (new Substitution(arguments[2], list.Root).TryUnify(out subs)) {
                    return new(Literals.True, subs.ToArray());
                }
            }
            return new(Literals.False);
        }
    }
}
