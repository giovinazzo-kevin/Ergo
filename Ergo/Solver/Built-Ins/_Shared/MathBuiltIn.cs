
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
        var context = (solver?.DecimalType ?? DecimalType.CliDecimal) switch
        {
            DecimalType.BigDecimal => EContext.Unlimited,
            DecimalType.Binary16 => EContext.Binary16,
            _ => EContext.CliDecimal,
        };
        return Evaluate(t);
        dynamic Evaluate(ITerm t)
        {
            if (t is Atom a) { return a.Value is EDecimal d ? d : Throw(a); }
            if (t is not Complex c) { return Throw(t); }

            return c.Functor switch
            {
                var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gt.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) > 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gte.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) >= 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lt.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) < 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lte.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) <= 0,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Modulo.Contains(f)
                => Remainder(c),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Addition.Contains(f)
                => Add(c),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Subtraction.Contains(f)
                => Subtract(c),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Multiplication.Contains(f)
                => Multiply(c),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Division.Contains(f)
                => Divide(c),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.IntDivision.Contains(f)
                => (((EDecimal)Evaluate(c.Arguments[0])).DivideToIntegerNaturalScale(Evaluate(c.Arguments[1]))),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Power.Contains(f)
                => ((EDecimal)Evaluate(c.Arguments[0])).Pow(Evaluate(c.Arguments[1])),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.SquareRoot.Contains(f)
                => ((EDecimal)Evaluate(c.Arguments[0])).Sqrt(null),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.AbsoluteValue.Contains(f)
                => ((EDecimal)Evaluate(c.Arguments[0])).Abs(),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Minus.Contains(f)
                => -Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Plus.Contains(f)
                => Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Round.Contains(f)
                => ((EDecimal)Evaluate(c.Arguments[0])).RoundToIntegerNoRoundedFlag(EContext.CliDecimal),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Floor.Contains(f)
                => EDecimal.FromInt64(((EDecimal)Evaluate(c.Arguments[0])).ToInt64Unchecked()),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Ceiling.Contains(f)
                => EDecimal.FromDecimal(Math.Ceiling(((EDecimal)Evaluate(c.Arguments[0])).ToDecimal())),
                _ => Throw(c)
            };
        }

        dynamic Divide(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            if (a is EDecimal A && b is EDecimal B)
            {
                return A.Divide(B, context);
            }
            return a / b;
        }

        dynamic Remainder(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            if (a is EDecimal A && b is EDecimal B)
            {
                return A.Remainder(B, context);
            }
            return a % b;
        }

        dynamic Add(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            if (a is EDecimal A && b is EDecimal B)
            {
                return A.Add(B, context);
            }
            return a + b;
        }

        dynamic Subtract(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            if (a is EDecimal A && b is EDecimal B)
            {
                return A.Subtract(B, context);
            }
            return a - b;
        }

        dynamic Multiply(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            if (a is EDecimal A && b is EDecimal B)
            {
                return A.Multiply(B, context);
            }
            return a * b;
        }

        double Throw(ITerm t) => throw new SolverException(SolverError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Number, t.Explain());
    }
}
