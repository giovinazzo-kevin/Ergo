namespace Ergo.Lang;

public static class Maybe
{
    public static Maybe<T> Some<T>(T some) => Maybe<T>.Some(some);
    public static Maybe<T> None<T>() => Maybe<T>.None;
}

public readonly struct Maybe<T>
{
    private readonly bool HasValue;
    private readonly T Value { get; }

    public Maybe<U> Map<U>(Func<T, Maybe<U>> some, Func<Maybe<U>> none = null)
    {
        if (HasValue)
        {
            return some(Value);
        }

        if (none != null)
        {
            return none();
        }

        return Maybe<U>.None;
    }

    public Maybe<U> Select<U>(Func<T, U> some, Func<U> none = null)
    {
        if (HasValue)
        {
            return some(Value);
        }

        if (none != null)
        {
            return none();
        }

        return Maybe<U>.None;
    }

    public U Reduce<U>(Func<T, U> some, Func<U> none)
    {
        if (HasValue)
        {
            return some(Value);
        }

        return none();
    }

    public bool TryGetValue(out T value)
    {
        if (HasValue) { value = Value; return true; }

        value = default;
        return false;
    }
    public T GetOr(T other)
    {
        if (HasValue)
            return Value;
        return other;
    }
    public Either<T, U> GetOr<U>(U other)
    {
        if (HasValue)
            return Value;
        return other;
    }

    public void Do(Action<T> some, Action none = null) => _ = Map<T>(v => { some(v); return default; }, () => { none?.Invoke(); return default; });

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    public static readonly Maybe<T> None = default;
    public static Maybe<T> Some(T value) => new(value);

    public static implicit operator Maybe<T>(T a) => Maybe.Some(a);
}
