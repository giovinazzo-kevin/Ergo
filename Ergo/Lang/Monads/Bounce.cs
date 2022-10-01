namespace Ergo.Lang;

public sealed class Bounce<T1, T2, TResult>
{
    public T1 Arg1 { get; private set; }
    public T2 Arg2 { get; private set; }
    public TResult Result { get; private set; }
    public bool HasResult { get; private set; }

    private Bounce() { }

    public static Bounce<T1, T2, TResult> Continue(T1 arg1, T2 arg2)
    {
        return new Bounce<T1, T2, TResult>()
        {
            Arg1 = arg1,
            Arg2 = arg2
        };
    }

    public static Bounce<T1, T2, TResult> End(TResult result)
    {
        return new Bounce<T1, T2, TResult>()
        {
            Result = result,
            HasResult = true
        };
    }
}

public static class Trampoline
{
    public static TResult Start<T1, T2, TResult>(Func<T1, T2, Bounce<T1, T2, TResult>> action,
      T1 arg1, T2 arg2)
    {
        TResult result;
        var bounce = Bounce<T1, T2, TResult>.Continue(arg1, arg2);
        while (true)
        {
            if (bounce.HasResult)
            {
                result = bounce.Result;
                break;
            }

            bounce = action(bounce.Arg1, bounce.Arg2);
        }

        return result;
    }
}