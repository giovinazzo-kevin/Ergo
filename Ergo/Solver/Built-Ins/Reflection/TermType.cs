using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class TermType : BuiltIn
    {
        public TermType()
            : base("", new("term_type"), Maybe<int>.Some(2), Modules.Reflection)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
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
                yield return new(Literals.True, new Substitution(arguments[1], type));
            }
            else if(new Substitution(arguments[1], type).TryUnify(out var subs))
            {
                yield return new(Literals.True, subs.ToArray());
            }
            else yield return new(Literals.False);
        }
    }
}
