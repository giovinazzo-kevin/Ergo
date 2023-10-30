namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound | InitializeAutomatically | EnableInliner | EnableCompiler,
    None = 0,
    /// <summary>
    /// If set, throws an error when a predicate is not found. If not set, the query fails silently instead.
    /// </summary>
    ThrowOnPredicateNotFound = 1,
    /// <summary>
    /// If set, the solver will initialize automatically on the first query. If not set, an error will be thrown if Solve() is called before initialization.
    /// </summary>
    InitializeAutomatically = 2,
    /// <summary>
    /// If set, predicates in the knowledge base will be inlined when the solver initializes. Safe optimization.
    /// </summary>
    EnableInliner = 4,
    /// <summary>
    /// If set, queries will be compiled just in time by the solver. Adds a slight initial overhead, but speeds up execution considerably.
    /// </summary>
    EnableCompiler = 8,
    /// <summary>
    /// If set, decimals will have no lower or upper bounds. If not set, decimals will behave like standard CLI decimals and eventually become +/-Infinity.
    /// </summary>
    UseUnboundedDecimals = 16
}
