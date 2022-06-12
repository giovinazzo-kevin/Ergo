using Ergo.Shell;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Ergo.Lang.Exceptions;

public readonly struct ExceptionHandler
{
    public readonly Action<ShellScope, Exception> Catch;
    public readonly Action Finally;

    public ExceptionHandler(Action<ShellScope, Exception> @catch, Action @finally = null)
    {
        Catch = @catch;
        Finally = @finally;
    }

    public void Throw(ShellScope scope, Exception e) => Try(scope, () => throw e);

    public bool Try(ShellScope scope, [NotNull] Action action)
    {
        Contract.Requires(action is { });

        try
        {
            action();
        }
        catch (Exception e)
        {
            Catch?.Invoke(scope, e);
            return false;
        }
        finally
        {
            Finally?.Invoke();
        }

        return true;
    }

    public async Task<bool> TryAsync(ShellScope scope, [NotNull] Func<Task> action)
    {
        Contract.Requires(action is { });

        try
        {
            await action();
        }
        catch (Exception e)
        {
            Catch?.Invoke(scope, e);
            return false;
        }
        finally
        {
            Finally?.Invoke();
        }

        return true;
    }

    public bool TryGet<T>(ShellScope scope, [NotNull] Func<T> func, out T value)
    {
        Contract.Requires(func is { });
        try
        {
            value = func();
        }
        catch (Exception e)
        {
            Catch?.Invoke(scope, e);
            value = default;
            return false;
        }
        finally
        {
            Finally?.Invoke();
        }

        return true;
    }

    public async Task<Maybe<T>> TryGetAsync<T>(ShellScope scope, [NotNull] Func<Task<T>> func)
    {
        Contract.Requires(func is { });
        try
        {
            return Maybe.Some(await func());
        }
        catch (Exception e)
        {
            Catch?.Invoke(scope, e);
            return Maybe.None<T>();
        }
        finally
        {
            Finally?.Invoke();
        }
    }
}

