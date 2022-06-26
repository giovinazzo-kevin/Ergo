using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;

namespace Ergo.Lang.Exceptions.Handler;
public struct ExceptionHandler
{
    public readonly Action<ErgoException> Catch;
    public readonly Action Finally;

    public event Action<ExceptionDispatchInfo> Throwing;
    public event Action<ExceptionDispatchInfo> Caught;

    public ExceptionHandler(Action<ErgoException> @catch, Action @finally = null)
    {
        Catch = @catch;
        Finally = @finally;
        Throwing = _ => { };
        Caught = _ => { };
    }

    public void Throw(ErgoException e) => Try(() => throw e);

    public bool Try([NotNull] Action action)
    {
        Contract.Requires(action is { });

        try
        {
            action();
        }
        catch (ErgoException e)
        {
            Catch?.Invoke(e);
            Caught?.Invoke(ExceptionDispatchInfo.Capture(e));
            return false;
        }
        catch (Exception e)
        {
            var dispatch = ExceptionDispatchInfo.Capture(e);
            Throwing?.Invoke(dispatch);
            dispatch.Throw();
        }
        finally
        {
            Finally?.Invoke();
        }

        return true;
    }

    public async Task<bool> TryAsync([NotNull] Func<Task> action)
    {
        Contract.Requires(action is { });

        try
        {
            await action();
        }
        catch (ErgoException e)
        {
            Catch?.Invoke(e);
            Caught?.Invoke(ExceptionDispatchInfo.Capture(e));
            return false;
        }
        catch (Exception e)
        {
            var dispatch = ExceptionDispatchInfo.Capture(e);
            Throwing?.Invoke(dispatch);
            dispatch.Throw();
        }
        finally
        {
            Finally?.Invoke();
        }

        return true;
    }

    public Maybe<T> TryGet<T>([NotNull] Func<T> func)
    {
        Contract.Requires(func is { });
        try
        {
            return func();
        }
        catch (ErgoException e)
        {
            Catch?.Invoke(e);
            Caught?.Invoke(ExceptionDispatchInfo.Capture(e));
        }
        catch (Exception e)
        {
            var dispatch = ExceptionDispatchInfo.Capture(e);
            Throwing?.Invoke(dispatch);
            dispatch.Throw();
        }
        finally
        {
            Finally?.Invoke();
        }

        return default;
    }

    public async Task<Maybe<T>> TryGetAsync<T>([NotNull] Func<Task<T>> func)
    {
        Contract.Requires(func is { });
        try
        {
            return await func();
        }
        catch (ErgoException e)
        {
            Catch?.Invoke(e);
            Caught?.Invoke(ExceptionDispatchInfo.Capture(e));
        }
        catch (Exception e)
        {
            var dispatch = ExceptionDispatchInfo.Capture(e);
            Throwing?.Invoke(dispatch);
            dispatch.Throw();
        }
        finally
        {
            Finally?.Invoke();
        }

        return Maybe.None<T>();
    }
}

