namespace Ergo.Runtime;

public enum VMMode
{
    /// <summary>
    /// Yields solutions interactively, one at a time. Ideal for a REPL environment.
    /// </summary>
    Interactive,
    /// <summary>
    /// Computes all solutions.
    /// </summary>
    Batch
}
