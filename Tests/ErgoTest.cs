using Ergo.Lang.Exceptions.Handler;

namespace Tests;

public abstract class ErgoTest
{
    public static readonly ExceptionHandler NullExceptionHandler = default;
    public static readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
}
