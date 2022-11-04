namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound | InitializeAutomatically
    , None = 0
    , ThrowOnPredicateNotFound = 1
    , InitializeAutomatically
}
