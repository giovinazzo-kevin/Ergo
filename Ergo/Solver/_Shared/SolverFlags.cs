﻿namespace Ergo.Solver;

[Flags]
public enum SolverFlags
{
    Default = ThrowOnPredicateNotFound | InitializeAutomatically | EnableInlining | EnableCompiler | EnableCompilerOptimizations,
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
    EnableInlining = 4,
    /// <summary>
    /// If set, queries will be compiled just in time by the solver. Adds a slight initial overhead, but speeds up execution considerably.
    /// </summary>
    EnableCompiler = 8,
    /// <summary>
    /// If set, compiled execution graphs will be optimized by performing compile-time checks. This greatly improves performance by discarding branches that are known to be false.
    /// </summary>
    EnableCompilerOptimizations = 16,
    /// <summary>
    /// If set, decimals will have no lower or upper bounds. If not set and UseFastDecimals is not set, decimals will behave like standard CLI decimals and eventually become +/-Infinity.
    /// </summary>
    UseUnboundedDecimals = 32,
    /// <summary>
    /// If set, decimals will use 16 bits instead of 96. If not set and UseUnboundedDecimals is not set, decimals will behave like standard CLI decimals and eventually become +/-Infinity. Has precedence over UseUnboundedDecimals.
    /// </summary>
    UseFastDecimals = 64
}
