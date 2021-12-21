using System;

namespace Ergo.Lang
{
    [Flags]
    public enum InterpreterFlags
    {
        Default = None,
        None = 0,
        AllowStaticModuleRedefinition = 1
    }
}
