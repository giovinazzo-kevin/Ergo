namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound | InitializeAutomatically | EnableInliner | EnableCompiler
    , None = 0
    , ThrowOnPredicateNotFound = 1
    , InitializeAutomatically
    , EnableInliner
    , EnableCompiler
}
