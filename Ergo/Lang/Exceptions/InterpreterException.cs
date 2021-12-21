using Ergo.Lang.Utils;
using System;

namespace Ergo.Lang
{
    public class InterpreterException : Exception
    {
        public InterpreterException(InterpreterError error, params object[] args)
            : base(ExceptionUtils.GetInterpreterError(error, args))
        {

        }
    }
}
