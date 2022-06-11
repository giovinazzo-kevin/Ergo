using Ergo.Lang.Utils;
using Ergo.Solver;
using System;

namespace Ergo.Lang.Exceptions
{
    public class SolverException : Exception
    {
        public SolverException(SolverError error, SolverScope scope, params object[] args)
            : base(ExceptionUtils.GetSolverError(error, scope, args))
        {
        }
    }
}
