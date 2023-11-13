namespace Ergo.Lang;
public static class Maybe
{
    public static Maybe<T> Some<T>(T some) => Maybe<T>.Some(some);
    public static Maybe<T> None<T>() => Maybe<T>.None;

    public static Maybe<T> FromTryGet<T>(Func<(bool, T)> tryGet)
    {
        var (success, value) = tryGet();
        if (success)
            return Some(value);
        return Maybe<T>.None;
    }
}

public readonly struct Maybe<T>
{
    public readonly bool HasValue;
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

    public Maybe<T> Where(Func<T, bool> cond)
    {
        if (HasValue && cond(Value))
            return this;
        return default;
    }

    public IEnumerable<T> AsEnumerable()
    {
        if (HasValue)
            yield return Value;
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
    public T GetOrLazy(Func<T> other)
    {
        if (HasValue)
            return Value;
        return other();
    }
    public Maybe<T> Or(Func<Maybe<T>> other)
    {
        if (HasValue)
            return this;
        return other();
    }

    public Maybe<T> And<U>(Func<Maybe<U>> other)
    {
        var this_ = this;
        return other().Map(_ => this_);
    }

    public T GetOrThrow(Exception ex)
    {
        if (HasValue)
            return Value;
        throw ex;
    }
    public Maybe<Either<T, U>> Either<U>(Func<Maybe<U>> other)
    {
        if (HasValue)
            return Maybe.Some<Either<T, U>>(Value);
        if (other().TryGetValue(out var v))
            return Maybe.Some<Either<T, U>>(v);
        return default;
    }
    public Either<T, U> GetEither<U>(U other)
    {
        if (HasValue)
            return Value;
        return other;
    }

    public Maybe<T> Do(Action<T> some = null, Action none = null) => Map<T>(v => { some?.Invoke(v); return v; }, () => { none?.Invoke(); return default; });
    public Maybe<T> Do(Action always = null) => Map<T>(v => { always?.Invoke(); return v; }, () => { always?.Invoke(); return default; });

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    public static readonly Maybe<T> None = default;
    public static Maybe<T> Some(T value) => new(value);

    public static implicit operator Maybe<T>(T a) => Maybe.Some(a);
}
