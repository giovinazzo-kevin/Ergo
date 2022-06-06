using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using Raylib_cs;
using static Ergo.Lang.Ast.WellKnown;

namespace Builtins
{

    public class blit : BuiltIn
    {

        public blit()
            : base("", new(nameof(blit)), Maybe.Some(2), new("ui"))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if(!args[0].Matches<Color>(out var rgba, matchFunctor: true))
            {
                yield return new Evaluation(Literals.False);
                yield break;
            }
            var o = origin.Value;
            var c = new Raylib_cs.Color(rgba.R, rgba.G, rgba.B, rgba.A);
            
            if (args[1].Matches<Point>(out var p, matchFunctor: true))
            {
                Render.Calls.Add(() => Raylib.DrawPixel(p.X - o.X, p.Y - o.Y, c));
                yield return new Evaluation(Literals.True);
            }
            else if (args[1].Matches<Line>(out var l, matchFunctor: true))
            {
                Render.Calls.Add(() => Raylib.DrawLine(l.Start.X - o.X, l.Start.Y - o.Y, l.End.X - o.X, l.End.Y - o.Y, c));
                yield return new Evaluation(Literals.True);
            }
            else if (args[1].Matches<Rectangle>(out var rect, matchFunctor: true))
            {
                Render.Calls.Add(() => Raylib.DrawRectangle(rect.Location.X - o.X, rect.Location.Y - o.Y, rect.Size.Width, rect.Size.Height, c));
                yield return new Evaluation(Literals.True);
            }
            else if (args[1].Matches<Circle>(out var circle, matchFunctor: true))
            {
                Render.Calls.Add(() => Raylib.DrawCircle(circle.Location.X - o.X, circle.Location.Y - o.Y, circle.Radius, c));
                yield return new Evaluation(Literals.True);
            }
            else yield return new Evaluation(Literals.False);
        }
    }
}