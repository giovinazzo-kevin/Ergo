using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using Raylib_cs;
using static Ergo.Lang.Ast.WellKnown;

namespace Builtins
{
    public class mouse : BuiltIn
    {
        public static Point Value = new(0, 0);

        public mouse()
            : base("", new(nameof(mouse)), Maybe.Some(1), new("ui"))
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (!args[0].IsGround)
            {
                Value = new(Raylib.GetMouseX(), Raylib.GetMouseY());
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