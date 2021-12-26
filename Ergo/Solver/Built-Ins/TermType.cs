using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class TermType : BuiltIn
    {
        public TermType()
            : base("", new("@term_type"), Maybe<int>.Some(2))
        {
        }

        public override Evaluation Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var type = arguments[0] switch
            {
                Atom => new Atom("atom"),
                Variable => new Atom("variable"),
                Complex => new Atom("complex"),
                _ => throw new NotSupportedException()
            };

            if (!arguments[1].IsGround)
            {
                return new(Literals.True, new Substitution(arguments[1], type));
            }
            if(new Substitution(arguments[1], type).TryUnify(out var subs))
            {
                return new(Literals.True, subs.ToArray());
            }
            return new(Literals.False);
        }
    }
}
