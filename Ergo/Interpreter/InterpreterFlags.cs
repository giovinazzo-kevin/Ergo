using System;

namespace Ergo.Interpreter
{
    [Flags]
    public enum InterpreterFlags
    {
        Default = None,
        None = 0,
        AllowStaticModuleRedefinition = 1
    }
}
