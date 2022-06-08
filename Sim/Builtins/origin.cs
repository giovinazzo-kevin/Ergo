using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using static Ergo.Lang.Ast.WellKnown;

namespace Builtins
{
    public class origin : BuiltIn
    {
        public static Point Value = new(0, 0);

        public origin()
            : base("", new(nameof(origin)), Maybe.Some(1), new("ui"))
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (!args[0].IsGround)
            {
                if (new Substitution(args[0], TermMarshall.ToTerm(Value)).TryUnify(out var subs))
                {
                    yield return new Evaluation(Literals.True, subs.ToArray());
                    yield break;
                }
            }
            else if(args[0].Matches(out Value))
            {
                if (new Substitution(args[0], TermMarshall.ToTerm(Value)).TryUnify(out var subs))
                {
                    yield return new Evaluation(Literals.True, subs.ToArray());
                    yield break;
                }
            }
            yield return new Evaluation(Literals.False);
        }
    }
}