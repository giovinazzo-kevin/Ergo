namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound | InitializeAutomatically | EnableInlining
    , None = 0
    , ThrowOnPredicateNotFound = 1
    , InitializeAutomatically
    , EnableInlining
}
