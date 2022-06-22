using Ergo.Lang.Utils;
using Ergo.Solver;

namespace Ergo.Lang.Exceptions;

public class SolverException : ErgoException
{
    public SolverException(SolverError error, SolverScope scope, params object[] args)
        : base(ExceptionUtils.GetSolverError(error, scope, args))
    {
    }
}
