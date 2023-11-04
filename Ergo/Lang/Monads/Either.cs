namespace Ergo.Lang;

public readonly struct Either<TA, TB>
{
    public readonly bool IsA { get; }
    private readonly TA A { get; }
    private readonly TB B { get; }

    public Either<TC, TD> Map<TC, TD>(Func<TA, TC> mapA, Func<TB, TD> mapB)
    {
        if (IsA)
        {
            return new Either<TC, TD>(mapA(A), default, IsA);
        }

        return new Either<TC, TD>(default, mapB(B), IsA);
    }

    public bool TryGetA(out TA a)
    {
        a = default;
        if (IsA) a = A;
        return IsA;
    }

    public bool TryGetB(out TB b)
    {
        b = default;
        if (!IsA) b = B;
        return !IsA;
    }

    public TC Reduce<TC>(Func<TA, TC> mapA, Func<TB, TC> mapB)
    {
        if (IsA)
        {
            return mapA(A);
        }

        return mapB(B);
    }
    public void Do(Action<TA> mapA, Action<TB> mapB)
    {
        if (IsA)
        {
            mapA(A);
        }
        else
        {
            mapB(B);
        }
    }

    private Either(TA a, TB b, bool isA)
    {
        A = a;
        B = b;
        IsA = isA;
    }

    public override bool Equals(object obj)
    {
        if (obj is Either<TA, TB> other)
        {
            if (other.IsA != IsA) return false;
            return IsA
                ? Equals(other.A, A)
                : Equals(other.B, B);
        }

        return false;
    }

    public static Either<TA, TB> FromA(TA a) => new(a, default, true);
    public static Either<TA, TB> FromB(TB b) => new(default, b, false);

    public override int GetHashCode() => IsA ? A.GetHashCode() : B.GetHashCode();

    public static implicit operator Either<TA, TB>(TA a) => FromA(a);
    public static implicit operator Either<TA, TB>(TB a) => FromB(a);
    public static implicit operator Either<TA, TB>(Either<TB, TA> a) => a.IsA ? FromB(a.A) : FromA(a.B);
    public static implicit operator TA(Either<TA, TB> a) => a.IsA ? a.A : default;
    public static implicit operator TB(Either<TA, TB> a) => a.IsA ? default : a.B;
}
