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
                => Remainder(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Addition.Contains(f)
                => Add(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Subtraction.Contains(f)
                => Subtract(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Multiplication.Contains(f)
                => Multiply(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Division.Contains(f)
                => Divide(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.IntDivision.Contains(f)
                => IntegerDivide(c, context),
                var f when c.Arguments.Length == 2 && WellKnown.Functors.Power.Contains(f)
                => Pow(c, context),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.SquareRoot.Contains(f)
                => (Evaluate(c.Arguments[0])).Sqrt(context),
                var f when c.Arguments.Length == 1 && WellKnown.Functors.AbsoluteValue.Contains(f)
                => (Evaluate(c.Arguments[0])).Abs(context),
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

        EDecimal Pow(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null)
                return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null)
                return EDecimal.NaN;
            var ret = a.Pow(b, ctx);
            return ret;
        }

        EDecimal IntegerDivide(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null)
                return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null)
                return EDecimal.NaN;
            var ret = a.DivideToIntegerNaturalScale(b, ctx);
            return ret;
        }

        EDecimal Divide(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null)
                return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null)
                return EDecimal.NaN;
            var ret = a.Divide(b, ctx);
            return ret;
        }

        EDecimal Remainder(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null)
                return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null)
                return EDecimal.NaN;
            return a.Remainder(b, ctx);
        }

        EDecimal Add(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null)
                return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null)
                return EDecimal.NaN;
            return a.Add(b, ctx);
        }

        EDecimal Subtract(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null) return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null) return EDecimal.NaN;
            return a.Subtract(b, ctx);
        }

        EDecimal Multiply(Complex c, EContext ctx)
        {
            var a = Evaluate(c.Arguments[0]);
            if (a is null) return EDecimal.NaN;
            var b = Evaluate(c.Arguments[1]);
            if (b is null) return EDecimal.NaN;
            return a.Multiply(b, ctx);
        }

        EDecimal Throw(ITerm t)
        {
            if (vm != null)
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, t.Explain());
            else throw new RuntimeException(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, t.Explain());
            return EDecimal.NaN;
        }
    }
}
