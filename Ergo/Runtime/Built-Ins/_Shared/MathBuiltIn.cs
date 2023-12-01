
using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public abstract class MathBuiltIn : BuiltIn
{
    public static readonly EDecimal DTrue = 1;
    public static readonly EDecimal DFalse = 0;

    protected MathBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Math)
    {
    }

    public EDecimal Evaluate(ErgoVM vm, ITerm t)
    {
        var context = (vm?.DecimalType ?? DecimalType.CliDecimal) switch
        {
            DecimalType.BigDecimal => EContext.Unlimited,
            DecimalType.FastDecimal => EContext.Binary16,
            _ => EContext.CliDecimal,
        };
        return Evaluate(t);
        EDecimal Evaluate(ITerm t)
        {
            if (t is Atom a) { return a.Value is EDecimal d ? d : Throw(a); }
            if (t is not Complex c) { return Throw(t); }

            return c.Functor switch
            {
                var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gt.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) > 0 ? DTrue : DFalse,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Gte.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) >= 0 ? DTrue : DFalse,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lt.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) < 0 ? DTrue : DFalse,
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Lte.Contains(f)
                => Evaluate(c.Arguments[0]).CompareTo(Evaluate(c.Arguments[1])) <= 0 ? DTrue : DFalse,
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
                => (Evaluate(c.Arguments[0]).DivideToIntegerNaturalScale(Evaluate(c.Arguments[1]))),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Power.Contains(f)
                => (Evaluate(c.Arguments[0])).Pow(Evaluate(c.Arguments[1])),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.SquareRoot.Contains(f)
                => (Evaluate(c.Arguments[0])).Sqrt(null),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.AbsoluteValue.Contains(f)
                => (Evaluate(c.Arguments[0])).Abs(),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Minus.Contains(f)
                => -Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Plus.Contains(f)
                => Evaluate(c.Arguments[0]),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Round.Contains(f)
                => (Evaluate(c.Arguments[0])).RoundToIntegerNoRoundedFlag(context),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Floor.Contains(f)
                => EDecimal.FromInt64((Evaluate(c.Arguments[0])).ToInt64Unchecked()),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.Ceiling.Contains(f)
                => EDecimal.FromDecimal(Math.Ceiling((Evaluate(c.Arguments[0])).ToDecimal())),
                _ => Throw(c)
            };
        }

        EDecimal Divide(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            return a.Divide(b, context);
        }

        EDecimal Remainder(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            return a.Remainder(b, context);
        }

        EDecimal Add(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            return a.Add(b, context);
        }

        EDecimal Subtract(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            return a.Subtract(b, context);
        }

        EDecimal Multiply(Complex c)
        {
            var a = Evaluate(c.Arguments[0]);
            var b = Evaluate(c.Arguments[1]);
            return a.Multiply(b, context);
        }

        EDecimal Throw(ITerm t)
        {
            if (vm != null)
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, t.Explain());
            else throw new RuntimeException(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, t.Explain());
            return default;
        }
    }
}
