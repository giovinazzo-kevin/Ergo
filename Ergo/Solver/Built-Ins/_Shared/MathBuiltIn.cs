
using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public abstract class MathBuiltIn : SolverBuiltIn
{
    protected MathBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Math)
    {
    }
    public dynamic Evaluate(ErgoSolver solver, SolverScope scope, ITerm t)
    {
        return Evaluate(solver, t);
        dynamic Evaluate(ErgoSolver solver, ITerm t)
        {
            if (t is Atom a) { return a.Value is EDecimal d ? d : Throw(a); }
            if (t is not Complex c) { return Throw(t); }

            return c.Functor switch
            {
                var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Evaluate(solver, c.Arguments[0]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gt.Contains(f)
                => Evaluate(solver, c.Arguments[0]).CompareTo(Evaluate(solver, c.Arguments[1])) > 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gte.Contains(f)
                => Evaluate(solver, c.Arguments[0]).CompareTo(Evaluate(solver, c.Arguments[1])) >= 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lt.Contains(f)
                => Evaluate(solver, c.Arguments[0]).CompareTo(Evaluate(solver, c.Arguments[1])) < 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lte.Contains(f)
                => Evaluate(solver, c.Arguments[0]).CompareTo(Evaluate(solver, c.Arguments[1])) <= 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Modulo.Contains(f)
                => Evaluate(solver, c.Arguments[0]) % Evaluate(solver, c.Arguments[1]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Addition.Contains(f)
                => Evaluate(solver, c.Arguments[0]) + Evaluate(solver, c.Arguments[1]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Subtraction.Contains(f)
                => Evaluate(solver, c.Arguments[0]) - Evaluate(solver, c.Arguments[1]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Multiplication.Contains(f)
                => Evaluate(solver, c.Arguments[0]) * Evaluate(solver, c.Arguments[1]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Division.Contains(f)
                => Evaluate(solver, c.Arguments[0]) / Evaluate(solver, c.Arguments[1]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.IntDivision.Contains(f)
                => (int)(Evaluate(solver, c.Arguments[0]) / Evaluate(solver, c.Arguments[1])),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Power.Contains(f)
                => Math.Pow(Evaluate(solver, c.Arguments[0]), Evaluate(solver, c.Arguments[1])),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.SquareRoot.Contains(f)
                => Math.Sqrt(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Minus.Contains(f)
                => -Evaluate(solver, c.Arguments[0]),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Plus.Contains(f)
                => +Evaluate(solver, c.Arguments[0]),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("sin"))
                => Math.Sin(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("cos"))
                => Math.Cos(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("tan"))
                => Math.Tan(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("sinh"))
                => Math.Sinh(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("cosh"))
                => Math.Cosh(Evaluate(solver, c.Arguments[0])),
                var f when c.Arguments.Length == 1 && f.Equals(new Atom("tanh"))
                => Math.Tanh(Evaluate(solver, c.Arguments[0])),
                _ => Throw(c)
            };
        }

        double Throw(ITerm t) => throw new SolverException(SolverError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Number, t.Explain());
    }
}
