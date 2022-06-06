using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using Raylib_cs;
using static Ergo.Lang.Ast.WellKnown;

namespace Builtins
{
    public class canvas : BuiltIn
    {
        public static Size Value = new(320, 320);

        public canvas()
            : base("", new(nameof(canvas)), Maybe.Some(1), new("ui"))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (args[0].Matches<Size>(out var newValue, matchFunctor: true))
            {
                Value = newValue;
                Raylib.SetWindowSize(Value.Width, Value.Height);
                yield return new Evaluation(Literals.True);
                yield break;
            }
            else if (!args[0].IsGround)
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