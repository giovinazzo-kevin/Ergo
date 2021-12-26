using Ergo.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Ergo.Lang.Exceptions
{
    public readonly struct ExceptionHandler
    {
        public readonly Action<ShellScope, Exception> Catch;
        public readonly Action Finally;

        public ExceptionHandler(Action<ShellScope, Exception> @catch, Action @finally = null)
        {
            Catch = @catch;
            Finally = @finally;
        }

        public bool Try(ShellScope scope, [NotNull] Action action)
        {
            Contract.Requires(action is { });

            try {
                action();
            }
            catch (Exception e) {
                Catch?.Invoke(scope, e);
                return false;
            }
            finally {
                Finally?.Invoke();
            }
            return true;
        }

        public bool TryGet<T>(ShellScope scope, [NotNull] Func<T> func, out T value)
        {
            Contract.Requires(func is { });
            try {
                value = func();
            }
            catch (Exception e) {
                Catch?.Invoke(scope, e);
                value = default;
                return false;
            }
            finally {
                Finally?.Invoke();
            }
            return true;
        }
    }

}
