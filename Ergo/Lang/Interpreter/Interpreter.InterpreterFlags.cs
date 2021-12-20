using System;

namespace Ergo.Lang
{
    public partial class Interpreter
    {
        [Flags]
        public enum InterpreterFlags
        {
            Default = None,
            None = 0,
            AllowStaticModuleRedefinition = 1
        }
    }
}
