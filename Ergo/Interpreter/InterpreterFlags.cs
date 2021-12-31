using System;

namespace Ergo.Interpreter
{
    [Flags]
    public enum InterpreterFlags
    {
        Default = ThrowOnDirectiveNotFound,
        None = 0,
        // Throws an exception instead of returning false if a directive is not found either when loading a module or at runtime.
        ThrowOnDirectiveNotFound = 1
    }
}
