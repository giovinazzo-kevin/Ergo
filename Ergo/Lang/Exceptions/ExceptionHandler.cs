using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Ergo.Lang
{
    public readonly struct ExceptionHandler
    {
        public readonly Action<Exception> Catch;
        public readonly Action Finally;

        public ExceptionHandler(Action<Exception> @catch, Action @finally = null)
        {
            Catch = @catch;
            Finally = @finally;
        }

        public bool Try([NotNull] Action action)
        {
            Contract.Requires(action is { });

            try {
                action();
            }
            catch (Exception e) {
                Catch?.Invoke(e);
                return false;
            }
            finally {
                Finally?.Invoke();
            }
            return true;
        }

        public bool TryGet<T>([NotNull] Func<T> func, out T value)
        {
            Contract.Requires(func is { });
            try {
                value = func();
            }
            catch (Exception e) {
                Catch?.Invoke(e);
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
