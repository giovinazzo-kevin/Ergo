namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound
    , None = 0
    , ThrowOnPredicateNotFound = 1
}
