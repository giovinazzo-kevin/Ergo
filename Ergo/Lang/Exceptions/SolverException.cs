using Ergo.Lang.Utils;
using Ergo.Solver;

namespace Ergo.Lang.Exceptions;

public class SolverException : ErgoException
{
    public readonly SolverError Error;
    public readonly object[] Args;

    public SolverException(SolverError error, SolverScope scope, params object[] args)
        : base(ExceptionUtils.GetSolverError(error, scope, args))
    {
        Error = error;
        Args = args;
    }
}
